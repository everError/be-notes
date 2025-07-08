# 트랜잭션 및 SaveChanges 재시도를 위한 Attribute 기반 구조 설계

## ✅ 개요

동시성 문제가 발생할 수 있는 환경에서 Entity Framework Core의 `SaveChanges` 동작을 감싸고, 자동 재시도와 트랜잭션 처리를 위해 Attribute 기반 구조를 설계할 수 있습니다. 이 방식은 ASP.NET Core, gRPC 서비스, 일반 Scoped 서비스 모두에 적용할 수 있으며, `ActionFilter`, `gRPC Interceptor`, `DispatchProxy`를 통해 구현됩니다.

---

## ✅ 목표

- 트랜잭션 자동 시작 및 커밋/롤백
- `DbUpdateConcurrencyException` 발생 시 **비즈니스 메서드 자체를 재실행**
- 재시도 후 성공 시 커밋, 실패 시 롤백
- Attribute 기반으로 선언적 사용

---

## ✅ 사용 기술 및 위치

| 대상                       | 적용 방식       | 설명                                              |
| -------------------------- | --------------- | ------------------------------------------------- |
| gRPC 서비스                | `Interceptor`   | 메서드 호출 전후를 감싸서 트랜잭션과 재시도 처리  |
| ASP.NET Core Controller    | `ActionFilter`  | MVC 액션 실행 전후에 트랜잭션 및 재시도 처리      |
| Scoped 서비스 (인터페이스) | `DispatchProxy` | 인터페이스 메서드 호출 시 트랜잭션 및 재시도 래핑 |

---

## ✅ Attribute 정의

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class SaveRetryAttribute : Attribute
{
    public Type DbContextType { get; }

    public SaveRetryAttribute(Type dbContextType)
    {
        DbContextType = dbContextType;
    }
}
```

---

## ✅ gRPC Interceptor 예시 (트랜잭션 외부에서 재시도 루프)

```csharp
public class SaveRetryInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var method = ResolveMethodInfo(context);
        var attr = method?.GetCustomAttribute<SaveRetryAttribute>();
        if (attr == null) return await continuation(request, context);

        var services = context.GetHttpContext()?.RequestServices;
        var dbContext = services?.GetService(attr.DbContextType) as DbContext;

        const int maxRetry = 10;
        Exception? lastException = null;

        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
            for (int retry = 0; retry < maxRetry; retry++)
            {
                try
                {
                    var result = await continuation(request, context);
                    await dbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    return result;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        var dbValues = await entry.GetDatabaseValuesAsync();
                        if (dbValues == null) throw;
                        entry.OriginalValues.SetValues(dbValues);
                    }
                    lastException = ex;
                    await Task.Delay(10);
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            await tx.RollbackAsync();
            throw new DbUpdateConcurrencyException("최대 재시도 초과", lastException);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

---

## ✅ ASP.NET Core ActionFilter 예시 (트랜잭션 외부에서 재시도 루프)

```csharp
public class SaveRetryFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _provider;
    public SaveRetryFilter(IServiceProvider provider) => _provider = provider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo;
        var attr = method?.GetCustomAttribute<SaveRetryAttribute>();
        if (attr == null)
        {
            await next();
            return;
        }

        var dbContext = _provider.GetService(attr.DbContextType) as DbContext;
        const int maxRetry = 10;
        Exception? lastException = null;

        await using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
            for (int retry = 0; retry < maxRetry; retry++)
            {
                try
                {
                    var executedContext = await next();
                    await dbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    return;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        var dbValues = await entry.GetDatabaseValuesAsync();
                        if (dbValues == null) throw;
                        entry.OriginalValues.SetValues(dbValues);
                    }
                    lastException = ex;
                    await Task.Delay(10);
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            await tx.RollbackAsync();
            throw new DbUpdateConcurrencyException("최대 재시도 초과", lastException);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

---

## ✅ Controller 예시

```csharp
[HttpPost]
[SaveRetry(typeof(MyDbContext))]
public async Task<IActionResult> UpdateSomething()
{
    var entity = await _context.MyEntities.FirstAsync();
    entity.Value++;
    return Ok();
}
```

---

## ✅ 등록 방법 (서비스 설정)

### gRPC Interceptor 등록

```csharp
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<SaveRetryInterceptor>();
});
```

### ActionFilter 등록 (전역 또는 조건부)

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<SaveRetryFilter>(); // 전역 등록
});

// 또는 개별 필터 주입을 위한 등록
builder.Services.AddScoped<SaveRetryFilter>();
```

---

## ✅ 인터페이스 서비스용 DispatchProxy 등록 방법 (Scoped 서비스)

```csharp
public class SaveRetryProxy<T> : DispatchProxy where T : class
{
    public required T Target { get; set; }
    public required DbContext DbContext { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;

        var attr = targetMethod.GetCustomAttribute<SaveRetryAttribute>();
        if (attr == null)
        {
            return targetMethod.Invoke(Target, args);
        }

        const int maxRetry = 10;
        for (int retry = 0; retry < maxRetry; retry++)
        {
            try
            {
                var result = targetMethod.Invoke(Target, args);

                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }

                DbContext.SaveChanges();
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    var dbValues = entry.GetDatabaseValues();
                    if (dbValues == null) throw;
                    entry.OriginalValues.SetValues(dbValues);
                }
                Thread.Sleep(10);
            }
        }

        throw new DbUpdateConcurrencyException("최대 재시도 초과");
    }

    public static T Create(T target, DbContext dbContext)
    {
        var proxy = Create<T, SaveRetryProxy<T>>();
        ((SaveRetryProxy<T>)(object)proxy).Target = target;
        ((SaveRetryProxy<T>)(object)proxy).DbContext = dbContext;
        return proxy;
    }
}
```

### DI 등록 예시

```csharp
services.AddScoped<IOrderService>(provider =>
{
    var dbContext = provider.GetRequiredService<MyDbContext>();
    var impl = provider.GetRequiredService<OrderService>();
    return SaveRetryProxy<IOrderService>.Create(impl, dbContext);
});
```

---

## ✅ 장점

- 재사용 가능한 Attribute 기반
- 비즈니스 로직은 수정 없이 유지
- gRPC, ASP.NET Core, 일반 Scoped 서비스 모두에 적용 가능

## ✅ 고려 사항

- Attribute로 지정한 `DbContext`는 DI 컨테이너에 등록되어 있어야 함
- 동시성 충돌 해결 로직은 `entry.GetDatabaseValues()` 기반으로 구현 가능
- `Interceptor`, `ActionFilter`, `DispatchProxy`는 각각 별도로 적용
- **트랜잭션은 재시도 루프 바깥에서 시작하여, Save 성공 시 Commit, 예외 시 Rollback 처리해야 함**
- **재시도 루프 내에서 `DbUpdateConcurrencyException` 외 예외 발생 시 즉시 Rollback 후 예외 전파되어야 함**
