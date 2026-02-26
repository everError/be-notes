# DbContext vs DbContextFactory

## DbContext는 thread-safe하지 않다

DbContext는 내부에 ChangeTracker, DB Connection 등 상태를 가지고 있고, 이 상태는 동시 접근에 대해 보호되지 않는다. 하나의 DbContext 인스턴스를 여러 Task에서 동시에 사용하면 예외가 발생한다.

```csharp
// ❌ 하나의 DbContext를 여러 Task에서 공유하면 예외 발생
var tasks = ids.Select(async id =>
{
    var item = await _context.Products.FindAsync(id);  // 💥
    await _context.SaveChangesAsync();
});
await Task.WhenAll(tasks);
```

---

## 등록 방식

```csharp
// DbContext만 Scoped로 등록
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));

// Factory + DbContext 둘 다 등록
services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(conn));
```

`AddDbContextFactory`를 사용하면 `IDbContextFactory<T>`와 `T(DbContext)` 모두 DI에 등록된다. 기존에 DbContext를 직접 주입받던 코드는 변경 없이 그대로 동작한다.

---

## Scoped DbContext의 특성

같은 Scope(요청) 안에서는 어디서 주입받든 같은 인스턴스다. ServiceA와 ServiceB가 같은 DbContext를 주입받으면 ChangeTracker도 공유되고, 한쪽에서 Add한 엔티티를 다른 쪽에서도 추적하고 있다.

```
HTTP Request (하나의 Scope)
├── ServiceA(AppDbContext) ──┐
├── ServiceB(AppDbContext) ──┼── 전부 같은 DbContext #1
└── ServiceC(AppDbContext) ──┘
    → Scope 끝나면 자동 Dispose
```

같은 인스턴스를 공유하기 때문에 여러 서비스에서 쌓은 변경사항을 `SaveChangesAsync()` 한 번으로 저장할 수 있다. 대신 이 서비스들을 `Task.WhenAll`로 병렬 호출하면 같은 인스턴스에 동시 접근하게 되어 터진다.

---

## Factory DbContext의 특성

`factory.CreateDbContextAsync()`를 호출할 때마다 완전히 새로운 인스턴스가 만들어진다. DI Scope와 무관하게 독립적이고, ChangeTracker도 각각 따로다. 커넥션도 풀에서 별도로 점유한다.

```
HTTP Request
├── factory.CreateDbContextAsync() → DbContext #A (독립)
├── factory.CreateDbContextAsync() → DbContext #B (독립)
└── factory.CreateDbContextAsync() → DbContext #C (독립)
    → 각각 await using으로 직접 Dispose
```

DI가 관리하지 않기 때문에 `await using`으로 직접 Dispose해야 한다. 독립 인스턴스라 병렬 사용은 안전하다.

---

## 비교 정리

| 구분           | DbContext 직접 주입              | IDbContextFactory                         |
| -------------- | -------------------------------- | ----------------------------------------- |
| 주입           | `AppDbContext context`           | `IDbContextFactory<AppDbContext> factory` |
| 인스턴스 생성  | DI가 Scope당 1개 자동 생성       | `CreateDbContextAsync()`로 직접 생성      |
| 수명 관리      | DI가 Scope 끝날 때 Dispose       | `await using`으로 직접 Dispose            |
| 같은 Scope에서 | 모든 서비스가 동일 인스턴스 공유 | 호출마다 새 인스턴스                      |
| ChangeTracker  | 공유                             | 인스턴스마다 독립                         |
| 커넥션         | Scope 내 1개                     | 인스턴스마다 풀에서 별도 점유             |
| 병렬 사용      | 불가                             | 가능                                      |

---

## 트랜잭션

EF Core에서 `SaveChangesAsync()`를 호출하면 그 시점에 ChangeTracker에 쌓인 변경사항들이 하나의 트랜잭션으로 실행된다. 이것이 EF Core가 자동으로 해주는 전부다.

```csharp
context.Orders.Add(order1);
context.Orders.Add(order2);
context.Products.Remove(product1);

// 이 세 개의 변경이 하나의 트랜잭션으로 실행됨
await context.SaveChangesAsync();
```

여러 번의 `SaveChangesAsync()`를 하나의 트랜잭션으로 묶고 싶으면 직접 트랜잭션을 관리해야 한다. 이건 Scoped든 Factory든 동일하다.

```csharp
await using var tx = await context.Database.BeginTransactionAsync();
try
{
    await context.SaveChangesAsync();  // 첫 번째 저장
    // 다른 작업...
    await context.SaveChangesAsync();  // 두 번째 저장

    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

Scoped DbContext의 경우 같은 Scope 안에서 여러 서비스가 동일 인스턴스를 공유하므로, 어디선가 한 번 `BeginTransactionAsync`를 호출하면 그 인스턴스를 쓰는 모든 곳이 같은 트랜잭션 안에 있게 된다.

Factory DbContext는 인스턴스마다 독립이므로 각각 별도로 트랜잭션을 관리해야 한다. 서로 다른 Factory 인스턴스의 트랜잭션을 하나로 묶으려면 `TransactionScope`가 필요하다.

---

## ChangeTracker가 분리된다는 것의 의미

Factory로 만든 두 개의 Context가 같은 레코드를 각각 조회하면 서로 다른 객체가 된다. 한쪽에서 수정하고 저장해도 다른 쪽은 모른다.

```csharp
await using var ctxA = await factory.CreateDbContextAsync();
await using var ctxB = await factory.CreateDbContextAsync();

var productA = await ctxA.Products.FindAsync(1);  // ctxA가 추적
var productB = await ctxB.Products.FindAsync(1);  // ctxB가 추적 (별개 객체)

productA.Price = 1000;
await ctxA.SaveChangesAsync();  // Price = 1000 저장

productB.Price = 2000;
await ctxB.SaveChangesAsync();  // Price = 2000으로 덮어씀 (Lost Update)
```

같은 레코드를 병렬로 수정할 가능성이 있다면 `[Timestamp]` RowVersion 같은 Concurrency Token으로 보호해야 한다.

---

## 사용 패턴

### 일반 CRUD — DbContext 직접 주입

```csharp
public class OrderService(AppDbContext context)
{
    public async Task CreateOrderAsync(Order order)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}
```

### 병렬 조회 — Factory

```csharp
public class DashboardService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var ordersTask = GetOrdersAsync();
        var productsTask = GetProductsAsync();
        await Task.WhenAll(ordersTask, productsTask);

        return new DashboardDto
        {
            Orders = ordersTask.Result,
            Products = productsTask.Result
        };
    }

    private async Task<List<Order>> GetOrdersAsync()
    {
        await using var ctx = await factory.CreateDbContextAsync();
        return await ctx.Orders.ToListAsync();
    }

    private async Task<List<Product>> GetProductsAsync()
    {
        await using var ctx = await factory.CreateDbContextAsync();
        return await ctx.Products.ToListAsync();
    }
}
```

### 혼용 — 조회는 Factory, 쓰기는 Scoped

```csharp
public class OrderService(
    AppDbContext context,
    IDbContextFactory<AppDbContext> factory)
{
    public async Task ProcessAsync(long id)
    {
        // 병렬 조회 (Factory)
        var infoTask = GetInfoAsync(id);
        var historyTask = GetHistoryAsync(id);
        await Task.WhenAll(infoTask, historyTask);

        // 쓰기 (Scoped)
        var order = await context.Orders.FindAsync(id);
        order!.Status = "Done";
        await context.SaveChangesAsync();
    }

    private async Task<OrderInfo> GetInfoAsync(long id)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        return await ctx.Orders.Where(o => o.Id == id).FirstOrDefaultAsync();
    }
}
```

### 배치 처리 — Parallel.ForEachAsync

```csharp
public async Task BulkProcessAsync(List<int> ids)
{
    await Parallel.ForEachAsync(ids,
        new ParallelOptions { MaxDegreeOfParallelism = 4 },
        async (id, ct) =>
        {
            await using var ctx = await factory.CreateDbContextAsync(ct);
            var item = await ctx.Products.FindAsync([id], ct);
            if (item is not null)
            {
                item.UpdatedAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync(ct);
            }
        });
}
```

---

## 주의사항

### Dispose 누락

Factory로 만든 DbContext는 DI가 관리하지 않으므로 반드시 `await using`으로 Dispose해야 한다. 누락하면 커넥션이 풀에 반환되지 않아 누수가 발생한다.

### 커넥션 풀 고갈

Factory로 만든 각 인스턴스가 커넥션을 하나씩 점유한다. 병렬도가 높으면 풀이 부족해질 수 있으므로 `MaxDegreeOfParallelism`을 제한하고, 필요시 ConnectionString에서 `Max Pool Size`를 조정해야 한다.

### 병렬이 효과적인 경우

각각 50ms 이상 걸리는 무거운 독립 쿼리가 여러 개이거나, 앱 서버와 DB 서버 간 네트워크 지연이 큰 환경, 또는 대량 배치 처리 시 효과적이다.

### 병렬이 불필요한 경우

`FindAsync(PK)` 같은 단순 조회(1~2ms)를 병렬로 돌리는 건 오버헤드 대비 이득이 없다. 순차적 의존성이 강한 로직(조회 → 검증 → 수정 → 저장)도 병렬화할 수 없다.

---

## 선택 기준

```
병렬로 실행할 독립적인 쿼리가 있는가?
├── NO → DbContext 직접 주입 (기존 방식)
└── YES → 각 쿼리가 충분히 무거운가? (50ms+)
    ├── NO → 순차 실행으로 충분 (기존 방식)
    └── YES → IDbContextFactory 사용
        ├── 읽기만 → Factory로 병렬 조회
        ├── 쓰기만 → Scoped DbContext
        └── 읽기 + 쓰기 → 혼용 (조회: Factory, 쓰기: Scoped)
```
