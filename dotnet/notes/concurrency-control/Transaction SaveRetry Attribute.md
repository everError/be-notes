# 트랜잭션 및 SaveChanges 재시도를 위한 Attribute 기반 구조 설계

## ✅ 개요

동시성 문제가 발생할 수 있는 환경에서 Entity Framework Core의 `SaveChanges` 동작을 감싸고, 자동 재시도와 트랜잭션 처리를 위해 Attribute 기반 구조를 설계할 수 있습니다. 이 방식은 ASP.NET Core, gRPC 서비스, 일반 Scoped 서비스 모두에 적용할 수 있으며, `ActionFilter`, `gRPC Interceptor`, `DispatchProxy`를 통해 구현됩니다.

---

## ✅ 목표

- 트랜잭션 자동 시작 및 커밋/롤백
- `DbUpdateConcurrencyException` 발생 시 **비즈니스 메서드 자체를 재실행**
- 재시도 후 성공 시 커밋, 실패 시 롤백
- Attribute 기반으로 선언적 사용
- **여러 `SaveRetryAttribute`가 선언된 경우에도 각 `DbContext`에 대해 별도로 트랜잭션 및 저장 처리**

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
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
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

## ✅ DbContext 예시

```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) {}

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<User> Users => Set<User>();
}
```

---

## ✅ 서비스 등록 예시

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<SaveRetryInterceptor>();
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<SaveRetryFilter>();
});

builder.Services.AddScoped<IOrderService>(provider =>
{
    var dbContexts = new List<DbContext>
    {
        provider.GetRequiredService<MyDbContext>(),
        // 필요한 다른 DbContext도 여기에 추가
    };

    var impl = provider.GetRequiredService<OrderService>();
    return SaveRetryProxy<IOrderService>.Create(impl, dbContexts);
});
```

---

## ✅ gRPC Interceptor 예시 (복수 Attribute 대응 및 트랜잭션 재시도)

```csharp
public class SaveRetryInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var attrs = MethodResolver.ResolveMethodAttributes<SaveRetryAttribute>(context).ToList();
        if (attrs.Count == 0)
            return await continuation(request, context);

        var services = context.GetHttpContext()?.RequestServices
            ?? throw new InvalidOperationException("HttpContext or RequestServices is unavailable.");

        var dbContexts = attrs
            .Select(attr => services.GetService(attr.DbContextType) as DbContext
                ?? throw new InvalidOperationException($"DbContext of type '{attr.DbContextType.FullName}' could not be resolved."))
            .Distinct()
            .ToList();

        const int maxRetry = 10;
        Exception? lastException = null;

        var transactions = new List<(DbContext Context, IDbContextTransaction Tx)>();
        try
        {
            foreach (var db in dbContexts)
            {
                var tx = await db.Database.BeginTransactionAsync();
                transactions.Add((db, tx));
            }

            for (int retry = 0; retry < maxRetry; retry++)
            {
                try
                {
                    var result = await continuation(request, context);

                    foreach (var (db, _) in transactions)
                        await db.SaveChangesAsync();

                    foreach (var (_, tx) in transactions)
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
                    foreach (var (_, tx) in transactions)
                        await tx.RollbackAsync();
                    throw;
                }
            }

            foreach (var (_, tx) in transactions)
                await tx.RollbackAsync();

            throw new DbUpdateConcurrencyException("Maximum retry count exceeded.", lastException);
        }
        finally
        {
            foreach (var (_, tx) in transactions)
            {
                await tx.DisposeAsync();
            }
        }
    }
}
```

---

## ✅ ASP.NET Core ActionFilter 예시 (복수 Attribute 대응 및 트랜잭션 재시도)

```csharp
public class SaveRetryFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _provider;
    public SaveRetryFilter(IServiceProvider provider) => _provider = provider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo;
        var attrs = method?.GetCustomAttributes<SaveRetryAttribute>().ToList();
        if (attrs == null || attrs.Count == 0)
        {
            await next();
            return;
        }

        var dbContexts = attrs
            .Select(attr => _provider.GetService(attr.DbContextType) as DbContext
                ?? throw new InvalidOperationException($"DbContext of type '{attr.DbContextType.FullName}' could not be resolved."))
            .Distinct()
            .ToList();

        const int maxRetry = 10;
        Exception? lastException = null;

        var transactions = new List<(DbContext Context, IDbContextTransaction Tx)>();
        try
        {
            foreach (var db in dbContexts)
            {
                var tx = await db.Database.BeginTransactionAsync();
                transactions.Add((db, tx));
            }

            for (int retry = 0; retry < maxRetry; retry++)
            {
                try
                {
                    var executedContext = await next();

                    foreach (var (db, _) in transactions)
                        await db.SaveChangesAsync();

                    foreach (var (_, tx) in transactions)
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
                    foreach (var (_, tx) in transactions)
                        await tx.RollbackAsync();
                    throw;
                }
            }

            foreach (var (_, tx) in transactions)
                await tx.RollbackAsync();

            throw new DbUpdateConcurrencyException("Maximum retry count exceeded.", lastException);
        }
        finally
        {
            foreach (var (_, tx) in transactions)
            {
                await tx.DisposeAsync();
            }
        }
    }
}
```

---

## ✅ DispatchProxy 기반 Scoped 서비스 예시 (복수 Attribute 대응)

```csharp
public class SaveRetryProxy<T> : DispatchProxy where T : class
{
    public required T Target { get; set; }
    public required List<DbContext> DbContexts { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;

        var attrs = targetMethod.GetCustomAttributes<SaveRetryAttribute>().ToList();
        if (attrs.Count == 0)
            return targetMethod.Invoke(Target, args);

        var dbContexts = attrs
            .Select(attr => DbContexts.FirstOrDefault(x => x.GetType() == attr.DbContextType)
                ?? throw new InvalidOperationException($"DbContext of type '{attr.DbContextType.FullName}' not available in proxy."))
            .Distinct()
            .ToList();

        var transactions = new List<(DbContext Context, IDbContextTransaction Tx)>();
        const int maxRetry = 10;

        for (int retry = 0; retry < maxRetry; retry++)
        {
            try
            {
                foreach (var db in dbContexts)
                {
                    var tx = db.Database.BeginTransaction();
                    transactions.Add((db, tx));
                }

                var result = targetMethod.Invoke(Target, args);

                if (result is Task task)
                    task.GetAwaiter().GetResult();

                foreach (var (db, _) in transactions)
                    db.SaveChanges();

                foreach (var (_, tx) in transactions)
                    tx.Commit();

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
            catch (Exception)
            {
                foreach (var (_, tx) in transactions)
                    tx.Rollback();
                throw;
            }
            finally
            {
                foreach (var (_, tx) in transactions)
                    tx.Dispose();
            }
        }

        throw new DbUpdateConcurrencyException("Maximum retry count exceeded");
    }

    public static T Create(T target, List<DbContext> dbContexts)
    {
        var proxy = Create<T, SaveRetryProxy<T>>();
        ((SaveRetryProxy<T>)(object)proxy).Target = target;
        ((SaveRetryProxy<T>)(object)proxy).DbContexts = dbContexts;
        return proxy;
    }
}
```

---

## ✅ 고려 사항

- `SaveRetryAttribute`가 여러 개 선언된 경우 **중복되지 않는 DbContext들 각각에 대해 트랜잭션과 저장을 별도로 수행**
- 모든 트랜잭션은 재시도 루프 안에서 명확하게 커밋 또는 롤백 처리되어야 함
- `entry.GetDatabaseValues()`는 삭제된 엔터티를 감지하여 예외로 처리할 수 있음
- 재시도 시점은 **비즈니스 로직 재실행 기준**이 아닌, `SaveChanges` 및 트랜잭션 기준임
- `DispatchProxy` 또한 다중 DbContext 지원을 위해 확장 가능
- `DispatchProxy` 구현 시 등록된 DbContext 인스턴스에서 `Attribute.DbContextType`과 매칭되는 인스턴스를 추출해야 함
