# EF Core LINQ 쿼리 작성 가이드

## 1. 기본 개념

### 1.1 Navigation Property란

Navigation Property는 EF Core에서 엔티티 간의 관계(FK)를 C# 클래스의 프로퍼티로 표현한 것입니다. DB 테이블 간의 외래 키 관계를 코드에서 직접 탐색할 수 있게 해줍니다.

```csharp
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    // Navigation Property: Order → Customer (1:1 참조)
    public Customer Customer { get; set; }

    // Navigation Property: Order → OrderItems (1:N 컬렉션)
    public List<OrderItem> OrderItems { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }

    // Navigation Property: Customer → Orders (1:N 컬렉션)
    public List<Order> Orders { get; set; }
}
```

`Customer`처럼 단일 엔티티를 가리키는 것을 Reference Navigation, `OrderItems`처럼 컬렉션을 가리키는 것을 Collection Navigation이라고 합니다.

Navigation Property가 중요한 이유는, LINQ에서 이 프로퍼티를 통해 관련 테이블의 데이터에 접근하면 EF Core가 자동으로 적절한 JOIN SQL을 생성해준다는 점입니다.

```csharp
// Navigation Property를 통해 접근 → EF Core가 JOIN을 생성
var result = await context.Orders
    .AsNoTracking()
    .Select(o => new
    {
        o.Id,
        CustomerName = o.Customer.Name,          // JOIN Customers
        ItemCount = o.OrderItems.Count()          // JOIN OrderItems
    })
    .ToListAsync();
```

이 가이드에서 "Navigation Property에 접근한다"라는 표현은 모두 이런 방식을 의미합니다.

### 1.2 IQueryable과 IEnumerable의 차이

EF Core 쿼리를 올바르게 작성하려면 이 두 인터페이스의 차이를 반드시 이해해야 합니다.

**IQueryable<T>** 는 쿼리 표현식 트리를 보유하고 있으며, 실행 메서드(`ToListAsync`, `FirstOrDefaultAsync` 등)가 호출되는 시점에 표현식 트리를 SQL로 변환하여 DB 서버에서 실행합니다. Where, OrderBy, Select 등 모든 연산이 SQL의 일부로 변환되므로 DB 서버의 인덱스와 최적화 엔진을 활용할 수 있습니다.

**IEnumerable<T>** 는 이미 메모리에 로드된 컬렉션에 대해 동작합니다. Where, OrderBy 등의 연산이 C# 코드로 메모리에서 실행되므로, 데이터가 이미 전부 DB에서 넘어온 상태입니다.

```csharp
// ✅ IQueryable: DB에서 필터링 (WHERE 절로 변환)
IQueryable<Order> query = context.Orders;
var active = await query
    .Where(o => o.Status == 1)    // SQL WHERE로 변환
    .ToListAsync();               // 이 시점에 SQL 실행

// ❌ IEnumerable: 전체 로드 후 메모리에서 필터링
IEnumerable<Order> all = await context.Orders.ToListAsync();  // 전체 테이블 로드
var active = all
    .Where(o => o.Status == 1)    // C#에서 메모리 필터링
    .ToList();
```

`AsEnumerable()`, `ToList()`, `ToArray()` 등을 호출하는 순간 IQueryable에서 IEnumerable로 전환됩니다. 이 전환 이후의 모든 LINQ 연산은 메모리에서 수행됩니다. 따라서 Where, OrderBy, Select 같은 연산은 반드시 IQueryable 상태에서(ToListAsync 호출 이전에) 체이닝해야 합니다.

다만 EF Core가 SQL로 변환할 수 없는 C# 고유 로직이 있는 경우에는, 서버에서 할 수 있는 필터링을 먼저 최대한 수행한 뒤에 `AsEnumerable()`로 전환하여 나머지를 메모리에서 처리하는 것이 허용됩니다.

```csharp
var result = context.Products
    .Where(p => p.IsActive && p.CategoryId == catId)  // DB 필터 (최대한 좁힌 후)
    .AsEnumerable()                                    // 전환
    .Where(p => CustomBusinessRule(p))                 // C# 로직
    .ToList();
```

---

## 2. 메서드 체이닝 기본 순서

EF Core에서 LINQ 쿼리를 작성할 때는 다음 순서를 기본 구조로 사용합니다.

```
AsNoTracking → Where → OrderBy/ThenBy → Select → ToListAsync
```

Include를 사용하는 경우에는 Select 대신 Include를 배치하며, 컬렉션 Include가 2개 이상이면 AsSplitQuery를 추가합니다.

```csharp
var result = await context.Products
    .AsNoTracking()
    .Where(p => p.IsActive)
    .OrderBy(p => p.CreatedAt)
    .Select(p => new { p.Id, p.Name, p.Price })
    .ToListAsync();
```

이 순서를 지키면 SQL이 의도한 대로 생성되고, 불필요한 데이터 로드나 클라이언트 평가를 방지할 수 있습니다.

---

## 3. Tracking 설정

### 3.1 읽기 전용 쿼리에는 AsNoTracking()을 명시합니다

`AsNoTracking()`은 EF Core의 Change Tracker가 해당 쿼리 결과 엔티티의 상태 변경을 추적하지 않도록 하는 메서드입니다. 추적에 필요한 내부 스냅샷 생성과 메모리 할당이 생략되므로 조회 성능이 향상됩니다.

조회만 하고 수정하지 않는 모든 쿼리에 `AsNoTracking()`을 붙여서 개발합니다.

```csharp
var orders = await context.Orders
    .AsNoTracking()
    .Where(o => o.Status == OrderStatus.Active)
    .ToListAsync();
```

### 3.2 AsNoTrackingWithIdentityResolution()

`AsNoTrackingWithIdentityResolution()`은 AsNoTracking과 마찬가지로 Change Tracker를 비활성화하되, 쿼리 결과 내에서 동일한 PK를 가진 엔티티가 여러 번 등장할 경우 하나의 인스턴스로 통합해주는 메서드입니다.

일반 AsNoTracking에서는 같은 PK의 엔티티라도 매번 새로운 인스턴스가 생성됩니다. Include를 통해 같은 엔티티가 여러 Navigation Property에서 참조되는 구조에서 이 메서드를 사용하면 메모리 사용량도 줄이고 참조 동일성도 유지할 수 있습니다.

```csharp
var books = await context.Books
    .AsNoTrackingWithIdentityResolution()
    .Include(b => b.Author)
    .Include(b => b.Coauthor)
    .ToListAsync();
// Author와 Coauthor가 같은 사람이면 동일 인스턴스로 통합
```

---

## 4. Where 절 배치

### 4.1 Where는 가능한 한 체인 앞쪽에 배치합니다

`Where()`는 SQL의 WHERE 절로 변환되어 DB 서버에서 행을 필터링하는 메서드입니다. 체인 앞쪽에 배치하면 이후 연산이 필터된 소규모 데이터에 대해서만 수행됩니다.

```csharp
var orders = await context.Orders
    .AsNoTracking()
    .Where(o => o.Status == 1)       // 먼저 필터
    .Include(o => o.Items)           // 필터된 행에 대해서만 Include
    .ToListAsync();
```

### 4.2 조건부 필터는 IQueryable 변수로 조합합니다

검색 조건이 선택적인 경우, IQueryable을 변수로 두고 조건이 있을 때만 Where를 추가하면 불필요한 조건 없이 깔끔한 SQL이 생성됩니다.

```csharp
var query = context.Products.AsNoTracking().AsQueryable();

if (!string.IsNullOrEmpty(keyword))
    query = query.Where(p => EF.Functions.Like(p.Name, $"%{keyword}%"));

if (minPrice.HasValue)
    query = query.Where(p => p.Price >= minPrice.Value);

if (categoryId.HasValue)
    query = query.Where(p => p.CategoryId == categoryId.Value);

var result = await query
    .OrderBy(p => p.Id)
    .ToListAsync();
```

---

## 5. Select 투영

### 5.1 전체 엔티티가 필요하지 않으면 Select로 필요한 필드만 투영합니다

`Select()`는 쿼리 결과를 원하는 형태로 변환(투영)하는 메서드로, SQL의 SELECT 절에 지정한 컬럼만 포함되도록 합니다. 불필요한 컬럼 전송을 줄이고, 투영 결과는 엔티티가 아니므로 자동으로 Change Tracker에서 제외됩니다.

```csharp
var users = await context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .Select(u => new UserListDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        OrderCount = u.Orders.Count()  // Navigation Property 접근 → 서브쿼리로 변환
    })
    .ToListAsync();
```

### 5.2 Select 사용 시 Include는 무시됩니다

Select 내부에서 Navigation Property에 직접 접근하면 EF Core가 필요한 JOIN을 자동 생성하므로, Select와 Include를 함께 쓸 필요가 없습니다.

```csharp
var orders = await context.Orders
    .AsNoTracking()
    .Where(o => o.Id == orderId)
    .Select(o => new
    {
        o.Id, o.OrderDate, o.Total,
        CustomerName = o.Customer.Name,             // Navigation Property → JOIN
        CustomerCity = o.Customer.Address.City,      // 중첩 Navigation → 다단계 JOIN
        Items = o.OrderItems.Select(i => new         // Collection Navigation → 서브쿼리
        {
            i.Quantity,
            ProductName = i.Product.Name
        })
    })
    .FirstOrDefaultAsync();
```

### 5.3 Select는 체인 마지막(실행 메서드 직전)에 배치합니다

Select를 앞쪽에 두면 투영된 결과에서 Navigation Property가 사라져 이후 Where나 OrderBy에서 원하는 조건을 걸 수 없게 됩니다. 필터와 정렬을 모두 마친 뒤 마지막에 Select를 적용합니다.

---

## 6. 관련 데이터 로딩

### 6.1 Include / ThenInclude

`Include()`는 Navigation Property의 데이터를 JOIN을 통해 한 번의 쿼리로 함께 로드하는 메서드(Eager Loading)입니다. `ThenInclude()`는 Include로 로드한 엔티티의 하위 Navigation Property를 추가로 로드할 때 사용합니다. N+1 쿼리 문제를 방지하는 핵심 수단입니다.

```csharp
var order = await context.Orders
    .AsNoTracking()
    .Include(o => o.OrderItems)          // Orders → OrderItems JOIN
        .ThenInclude(oi => oi.Product)   // OrderItems → Products JOIN
    .Include(o => o.Customer)            // Orders → Customers JOIN
    .FirstOrDefaultAsync(o => o.Id == orderId);
```

단, Include를 3단계 이상 깊게 중첩하면 거대한 JOIN이 생성되어 오히려 느려질 수 있습니다. 깊은 중첩이 필요한 경우에는 Select 투영으로 대체하는 것이 대부분 더 효율적입니다.

### 6.2 Filtered Include (EF Core 5.0+)

Include 내부에서 `Where()`, `OrderBy()`, `Take()` 등을 적용하여 관련 컬렉션 데이터를 조건부로 로드할 수 있는 기능입니다. 전체 컬렉션을 가져오지 않고 필요한 부분만 가져오므로 전송 데이터가 줄어듭니다.

```csharp
var customer = await context.Customers
    .AsNoTracking()
    .Include(c => c.Orders
        .Where(o => o.Status != OrderStatus.Cancelled)
        .OrderByDescending(o => o.OrderDate)
        .Take(5))
    .FirstOrDefaultAsync(c => c.Id == customerId);
```

### 6.3 AsSplitQuery

`AsSplitQuery()`는 하나의 거대한 JOIN 쿼리 대신 각 Include를 별도의 SELECT 쿼리로 분리하여 실행하는 메서드입니다. 컬렉션 Navigation Property를 2개 이상 Include할 때 발생하는 Cartesian Explosion(데카르트 곱)을 방지합니다.

```csharp
var blogs = await context.Blogs
    .AsNoTracking()
    .Include(b => b.Posts)
    .Include(b => b.Tags)
    .AsSplitQuery()
    .ToListAsync();
// → SELECT Blogs / SELECT Posts / SELECT Tags 3회로 분리
```

컬렉션 Include가 1개뿐이면 단일 쿼리가 더 효율적이므로, AsSplitQuery는 컬렉션 2개 이상일 때 적용합니다. Split Query는 여러 번의 DB 왕복이 발생하고 쿼리 간 데이터 일관성이 보장되지 않으므로, 강한 일관성이 필요하면 트랜잭션 격리 수준을 함께 고려해야 합니다.

---

## 7. 단건 조회 메서드 선택

**FindAsync(key)** — DbContext 내부의 1차 캐시(Identity Map)를 먼저 확인하고, 이미 트래킹 중인 엔티티가 있으면 DB 조회 없이 즉시 반환하는 메서드입니다. PK 기반 조회에 최적화되어 있습니다. 단, AsNoTracking과 함께 사용할 수 없으므로, 읽기 전용 조회에서는 `FirstOrDefaultAsync`에 PK 조건을 거는 방식을 사용합니다.

```csharp
// 엔티티 수정이 필요한 경우 → FindAsync
var user = await context.Users.FindAsync(userId);
```

**FirstOrDefaultAsync(predicate)** — 조건에 맞는 첫 번째 행을 반환하며, 없으면 null을 반환하는 메서드입니다. SQL에서 TOP 1로 변환됩니다. 대부분의 조건부 단건 조회에 사용합니다.

```csharp
var user = await context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Email == email);
```

**SingleOrDefaultAsync(predicate)** — 결과가 정확히 0개 또는 1개인지 검증하며, 2개 이상이면 예외를 발생시키는 메서드입니다. 내부적으로 TOP 2를 조회하여 유일성을 검증하므로 FirstOrDefaultAsync보다 약간 비용이 높습니다. 비즈니스 규칙상 유일성이 보장되어야 하는 값에 사용합니다.

```csharp
var setting = await context.Settings
    .AsNoTracking()
    .SingleOrDefaultAsync(s => s.Key == key);
```

---

## 8. EF.Functions 활용

`EF.Functions`는 C# LINQ 표현식에서 데이터베이스 고유 함수를 호출할 수 있도록 EF Core가 제공하는 정적 헬퍼 클래스입니다. 여기에 포함된 메서드들은 SQL로 직접 변환되어 DB 서버에서 실행되므로, 클라이언트 평가를 방지하고 DB 엔진의 최적화를 활용할 수 있습니다.

### 8.1 Like — SQL LIKE 패턴 매칭

`EF.Functions.Like(column, pattern)`는 SQL의 LIKE 연산자로 변환되며, 와일드카드(`%`, `_`)를 사용한 패턴 매칭을 수행합니다. `string.Contains()`도 LIKE로 변환되긴 하지만, Like를 직접 사용하면 패턴을 명시적으로 제어할 수 있습니다. `"keyword%"` 패턴은 인덱스를 활용할 수 있어 `"%keyword%"` 대비 훨씬 빠릅니다.

```csharp
// 앞쪽 일치: 인덱스 활용 가능
context.Products.Where(p => EF.Functions.Like(p.Name, "key%"))

// 부분 일치: 인덱스 활용 불가, 풀스캔 발생
context.Products.Where(p => EF.Functions.Like(p.Name, "%key%"))
```

### 8.2 DateDiffDay — 서버 사이드 날짜 차이 계산

`EF.Functions.DateDiffDay(start, end)`는 SQL Server의 DATEDIFF 함수로 변환되어 두 날짜 간의 일수 차이를 DB 서버에서 계산합니다. C# DateTime 연산은 클라이언트 평가를 유발하거나 비효율적인 SQL을 생성할 수 있으므로 DateDiff 계열을 사용합니다. DateDiffHour, DateDiffMinute, DateDiffMonth 등 다양한 단위가 제공됩니다.

```csharp
var recent = await context.Orders
    .AsNoTracking()
    .Where(o => EF.Functions.DateDiffDay(o.OrderDate, DateTime.UtcNow) <= 30)
    .ToListAsync();
// → WHERE DATEDIFF(day, [OrderDate], GETUTCDATE()) <= 30
```

### 8.3 FreeText / Contains — Full-Text Search

`EF.Functions.FreeText(column, searchText)`는 SQL Server의 FREETEXT 함수로 변환되어 Full-Text Index가 설정된 컬럼에 대해 자연어 기반 전문 검색을 수행합니다. `EF.Functions.Contains()`는 더 정밀한 검색 구문(AND, OR, NEAR 등)을 지원합니다.

```csharp
var articles = await context.Articles
    .AsNoTracking()
    .Where(a => EF.Functions.FreeText(a.Content, "machine learning AI"))
    .ToListAsync();
```

### 8.4 Collate — 정렬/비교 규칙 지정

`EF.Functions.Collate(column, collation)`는 특정 Collation을 지정하여 비교하는 메서드입니다. 대소문자 구분 비교가 필요한 경우 등에 사용합니다.

```csharp
var users = await context.Users
    .AsNoTracking()
    .Where(u => EF.Functions.Collate(
        u.Username, "SQL_Latin1_General_CP1_CS_AS") == input)
    .ToListAsync();
```

---

## 9. ToListAsync 호출 시점

`ToListAsync()`, `FirstOrDefaultAsync()`, `CountAsync()` 같은 실행 메서드는 IQueryable의 표현식 트리를 SQL로 변환하고 DB에 전송하여 결과를 반환하는 최종 트리거입니다. 호출 전까지는 IQueryable 상태로 쿼리 조합만 진행되고, 호출 시점에 비로소 SQL이 실행됩니다.

따라서 실행 메서드는 모든 필터, 정렬, 투영을 완료한 후 체인의 맨 끝에서 호출해야 합니다. 너무 이른 시점에 `ToListAsync()`를 호출하면 전체 데이터를 메모리에 올린 후 나머지 처리가 C# 메모리에서 수행됩니다.

---

## 10. 존재 여부 확인과 집계

### 10.1 존재 여부는 AnyAsync를 사용합니다

`AnyAsync(predicate)`는 SQL의 EXISTS로 변환되어 조건에 맞는 행이 하나라도 있는지 확인하는 메서드입니다. 첫 번째 매칭 행을 찾는 즉시 true를 반환합니다. `CountAsync() > 0`은 전체 행 수를 세므로 존재 여부 확인에는 AnyAsync가 훨씬 효율적입니다.

```csharp
if (await context.Orders.AnyAsync(o => o.CustomerId == id))
{
    // 처리
}
```

### 10.2 집계는 DB에서 수행합니다

`CountAsync()`, `SumAsync()`, `AverageAsync()`, `MaxAsync()`, `MinAsync()` 등의 집계 메서드는 SQL의 집계 함수로 변환되어 DB 서버에서 계산을 수행합니다. `ToListAsync()` 후에 LINQ to Objects로 집계하면 전체 데이터를 메모리에 올리게 되므로 반드시 IQueryable 상태에서 호출합니다.

```csharp
var totalRevenue = await context.Orders
    .Where(o => o.Status == OrderStatus.Completed)
    .SumAsync(o => o.Total);
```

---

## 11. 반복문 내 조회 방지 — 한 번 조회 후 자료구조로 탐색

여러 테이블의 데이터를 순회하면서 관계를 확인하거나 매칭하는 로직에서, 반복문 내부에서 매번 DB 조회를 하면 건수만큼 쿼리가 발생합니다. 필요한 데이터를 미리 한 번에 조회한 뒤 `Dictionary`, `HashSet`, `ToLookup` 등의 자료구조로 변환하여 메모리에서 O(1) 탐색하는 것이 핵심입니다.

### 11.1 기본 원칙

```csharp
// ❌ 반복문 내부에서 매번 DB 조회
foreach (var order in orders)
{
    var customer = await context.Customers
        .FirstOrDefaultAsync(c => c.Id == order.CustomerId);  // 주문 건수만큼 쿼리
    var warehouse = await context.Warehouses
        .FirstOrDefaultAsync(w => w.RegionId == customer.RegionId);  // 또 주문 건수만큼
}

// ✅ 필요한 데이터를 먼저 전부 조회 → 자료구조로 변환 → 메모리에서 탐색
```

### 11.2 실무 예시: 주문 일괄 처리

주문 목록을 순회하면서 상품별 가격 정책 적용, 고객 등급별 할인율 계산, 재고 차감 가능 여부 확인을 동시에 수행해야 하는 상황입니다.

```csharp
public async Task<List<OrderCalculationResult>> CalculateOrdersAsync(List<int> orderIds)
{
    // ──────────────────────────────────────
    // 1단계: 필요한 데이터를 테이블별로 한 번씩만 조회
    // ──────────────────────────────────────
    var orders = await _context.Orders
        .AsNoTracking()
        .Where(o => orderIds.Contains(o.Id))
        .Include(o => o.OrderItems)
        .ToListAsync();

    // 이후 조회에 필요한 ID 목록을 미리 수집
    var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
    var productIds = orders
        .SelectMany(o => o.OrderItems)
        .Select(oi => oi.ProductId)
        .Distinct()
        .ToList();

    // 각 테이블을 한 번씩만 조회하여 자료구조로 변환
    // Dictionary<int, Customer>: 고객 ID → 고객 정보
    var customerMap = await _context.Customers
        .AsNoTracking()
        .Where(c => customerIds.Contains(c.Id))
        .ToDictionaryAsync(c => c.Id);

    // Dictionary<int, Product>: 상품 ID → 상품 정보 (가격 포함)
    var productMap = await _context.Products
        .AsNoTracking()
        .Where(p => productIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);

    // Dictionary<int, int>: 상품 ID → 현재 재고 수량
    var stockMap = await _context.Inventories
        .AsNoTracking()
        .Where(i => productIds.Contains(i.ProductId))
        .ToDictionaryAsync(i => i.ProductId, i => i.StockQuantity);

    // Dictionary<string, decimal>: 등급명 → 할인율
    var discountRateMap = await _context.GradeDiscounts
        .AsNoTracking()
        .ToDictionaryAsync(g => g.GradeName, g => g.DiscountRate);

    // HashSet<int>: 현재 프로모션 적용 중인 상품 ID 집합
    var promoProductIds = (await _context.Promotions
        .AsNoTracking()
        .Where(p => p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow)
        .Select(p => p.ProductId)
        .ToListAsync())
        .ToHashSet();

    // ──────────────────────────────────────
    // 2단계: 메모리에서 순회하며 비즈니스 로직 수행 (DB 호출 0회)
    // ──────────────────────────────────────
    var results = new List<OrderCalculationResult>();

    foreach (var order in orders)
    {
        var customer = customerMap[order.CustomerId];
        var discountRate = discountRateMap.GetValueOrDefault(customer.GradeName, 0m);

        var orderResult = new OrderCalculationResult
        {
            OrderId = order.Id,
            CustomerName = customer.Name
        };

        foreach (var item in order.OrderItems)
        {
            var product = productMap[item.ProductId];
            var stock = stockMap.GetValueOrDefault(item.ProductId, 0);

            // 재고 확인
            if (item.Quantity > stock)
            {
                orderResult.Warnings.Add(
                    $"{product.Name}: 재고 부족 (요청 {item.Quantity}, 재고 {stock})");
                continue;
            }

            // 가격 계산: 기본가 × 수량
            var lineTotal = product.UnitPrice * item.Quantity;

            // 프로모션 상품이면 10% 추가 할인
            if (promoProductIds.Contains(item.ProductId))
                lineTotal *= 0.9m;

            // 고객 등급 할인 적용
            lineTotal *= (1 - discountRate);

            orderResult.Items.Add(new LineItemResult
            {
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice,
                FinalTotal = lineTotal
            });
        }

        orderResult.GrandTotal = orderResult.Items.Sum(i => i.FinalTotal);
        results.Add(orderResult);
    }

    return results;
}
```

이 예시에서 DB 조회는 Orders, Customers, Products, Inventories, GradeDiscounts, Promotions 총 6회로 고정됩니다. 주문 1000건 × 상품 5000건이어도 DB 호출 횟수는 변하지 않으며, 반복문 내부의 모든 탐색은 Dictionary/HashSet에서 O(1)로 처리됩니다.

만약 자료구조 변환 없이 반복문 내부에서 매번 조회했다면, 주문 수 × 상품 수만큼 쿼리가 발생하여 수천~수만 회의 DB 왕복이 생깁니다.

### 11.3 자료구조 선택 기준

**Dictionary<TKey, TValue>** — 키 하나에 값 하나가 매핑되는 경우에 사용합니다. ID로 엔티티를 찾거나, 특정 키에 대응하는 단일 값(가격, 수량, 이름 등)을 조회할 때 적합합니다.

```csharp
// 상품 ID → 상품 엔티티
var productMap = await context.Products
    .AsNoTracking()
    .Where(p => productIds.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id);

// 상품 ID → 재고 수량 (엔티티가 아닌 특정 값만 필요할 때)
var stockMap = await context.Inventories
    .AsNoTracking()
    .Where(i => productIds.Contains(i.ProductId))
    .ToDictionaryAsync(i => i.ProductId, i => i.StockQuantity);
```

**HashSet<T>** — 특정 값이 집합에 포함되는지 여부만 확인하면 되는 경우에 사용합니다. "이 상품이 할인 대상인가", "이 사용자가 블랙리스트인가" 같은 존재 여부 판단에 적합합니다.

```csharp
// 할인 대상 상품 ID 집합
var discountedIds = (await context.Discounts
    .AsNoTracking()
    .Select(d => d.ProductId)
    .Distinct()
    .ToListAsync())
    .ToHashSet();

// 탐색: Contains O(1)
if (discountedIds.Contains(product.Id)) { ... }
```

**ToLookup<TKey, TElement>** — 키 하나에 여러 값이 매핑되는 1:N 관계에서, 이미 메모리에 로드된 데이터를 그룹핑할 때 사용합니다. DB에서 GroupBy로 해결할 수 있는 단순 그룹핑과는 달리, 조회 후 여러 번 교차 참조해야 하는 상황에서 유용합니다.

```csharp
// 주문별 클레임 목록을 미리 그룹핑 (이후 주문 순회 시 반복 참조)
var claimsByOrder = (await context.Claims
    .AsNoTracking()
    .Where(c => orderIds.Contains(c.OrderId))
    .ToListAsync())
    .ToLookup(c => c.OrderId);   // ILookup<int, Claim>

foreach (var order in orders)
{
    var claims = claimsByOrder[order.Id];  // O(1), 없으면 빈 컬렉션
    // claims를 기반으로 환불 금액 계산, 상태 판정 등
}
```

---

## 12. 페이지네이션

우리 프로젝트에서는 Virtual Scroll 기반으로 조회하는 경우가 대부분이므로 페이지네이션을 자주 사용하지는 않지만, 필요한 경우 다음 방식을 참고합니다.

### 12.1 Offset 기반 (Skip/Take)

`Skip(n)`은 SQL의 OFFSET, `Take(n)`은 FETCH NEXT(또는 TOP)로 변환되어 지정된 범위의 행만 반환합니다. 구현이 간단하고 임의 페이지 접근이 가능하지만, 페이지가 깊어질수록 Skip할 행을 스캔해야 하므로 대규모 데이터에서 느려집니다. 반드시 `OrderBy`를 Skip/Take 이전에 배치해야 하며, 정렬 없이 페이징하면 결과 순서가 보장되지 않아 중복·누락이 발생할 수 있습니다.

```csharp
var page = await context.Products
    .AsNoTracking()
    .OrderBy(p => p.Id)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 12.2 Keyset 기반 (Cursor)

이전 페이지의 마지막 키 값을 기준으로 다음 데이터를 조회하는 방식입니다. 인덱스를 활용하여 페이지 깊이와 관계없이 일정한 성능을 유지하며, 데이터 변경 중에도 중복이나 누락이 발생하지 않습니다. 대규모 데이터에서 페이지네이션이 필요하다면 이 방식을 권장합니다.

```csharp
var nextPage = await context.Products
    .AsNoTracking()
    .OrderBy(p => p.Id)
    .Where(p => p.Id > lastSeenId)
    .Take(pageSize)
    .ToListAsync();
```

---

## 13. 벌크 연산 (EF Core 7.0+)

### 13.1 ExecuteUpdateAsync

`ExecuteUpdateAsync()`는 Where로 필터된 행에 대해 Change Tracker를 거치지 않고 단일 UPDATE SQL을 직접 실행하는 메서드입니다. 엔티티를 하나씩 로드하여 수정하고 SaveChanges를 호출하는 방식 대비 수천 배 이상 빠를 수 있습니다.

```csharp
await context.Products
    .Where(p => p.CategoryId == categoryId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(p => p.Price, p => p.Price * 1.1m)
        .SetProperty(p => p.ModifiedAt, DateTime.UtcNow));
```

### 13.2 ExecuteDeleteAsync

`ExecuteDeleteAsync()`는 Where로 필터된 행을 단일 DELETE SQL로 직접 삭제하는 메서드입니다.

```csharp
await context.AuditLogs
    .Where(l => l.CreatedAt < DateTime.UtcNow.AddYears(-1))
    .ExecuteDeleteAsync();
```

벌크 연산은 Change Tracker를 우회하므로, 동일 DbContext에서 이미 트래킹 중인 엔티티와 상태가 동기화되지 않습니다. 벌크 연산 이후에 해당 엔티티를 다시 조회해야 한다면 `ChangeTracker.Clear()`를 호출하거나 새로운 DbContext를 사용합니다.

---

## 14. 비즈니스 로직 작성 패턴

### 14.1 기본 흐름: 조회 → 매핑 → 반환

서비스 레이어에서 비즈니스 로직을 작성할 때는 DB 조회와 비즈니스 로직/매핑을 분리하는 흐름을 따릅니다. DB에는 데이터 조회만 맡기고, 포맷팅·계산·상태 변환 등의 가공은 메모리에 가져온 이후에 수행합니다.

```csharp
public async Task<OrderDetailResponse> GetOrderDetailAsync(int orderId)
{
    // 1단계: DB 조회 (필요한 데이터만 Select로 가져옴)
    var order = await _context.Orders
        .AsNoTracking()
        .Where(o => o.Id == orderId)
        .Select(o => new
        {
            o.Id, o.OrderDate, o.Status, o.Total,
            CustomerName = o.Customer.Name,
            Items = o.OrderItems.Select(i => new
            {
                i.Quantity, i.UnitPrice,
                ProductName = i.Product.Name
            }).ToList()
        })
        .FirstOrDefaultAsync();

    if (order is null)
        throw new NotFoundException($"Order {orderId} not found");

    // 2단계: 비즈니스 로직 및 매핑 (메모리에서 수행)
    return new OrderDetailResponse
    {
        OrderId = order.Id,
        OrderDate = order.OrderDate,
        StatusText = order.Status.ToDisplayString(),
        CustomerName = order.CustomerName,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SubTotal = i.Quantity * i.UnitPrice
        }).ToList(),
        TotalFormatted = order.Total.ToString("C")
    };
}
```

### 14.2 수정 작업: 조회(Tracking) → 수정 → SaveChanges

엔티티를 수정할 때는 AsNoTracking 없이 조회하여 Change Tracker가 추적하도록 한 뒤, 프로퍼티를 변경하고 SaveChangesAsync를 호출합니다.

```csharp
public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
{
    // 1단계: Tracking 조회 (AsNoTracking 없이)
    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order is null)
        throw new NotFoundException($"Order {orderId} not found");

    // 2단계: 비즈니스 검증
    if (order.Status == OrderStatus.Cancelled)
        throw new BusinessException("취소된 주문의 상태는 변경할 수 없습니다.");

    // 3단계: 수정
    order.Status = newStatus;
    order.ModifiedAt = DateTime.UtcNow;

    // 4단계: 저장 (변경된 필드만 UPDATE SQL 생성)
    await _context.SaveChangesAsync();
}
```

### 14.3 목록 조회: 조건 조합 → 투영 → 반환

목록 조회에서는 IQueryable로 조건을 조합한 뒤, Select로 DTO에 직접 투영하여 반환합니다.

```csharp
public async Task<List<ProductListDto>> GetProductsAsync(ProductSearchRequest request)
{
    var query = _context.Products.AsNoTracking().AsQueryable();

    if (!string.IsNullOrEmpty(request.Keyword))
        query = query.Where(p => EF.Functions.Like(p.Name, $"%{request.Keyword}%"));

    if (request.CategoryId.HasValue)
        query = query.Where(p => p.CategoryId == request.CategoryId.Value);

    return await query
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new ProductListDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            CategoryName = p.Category.Name,
            CreatedAt = p.CreatedAt
        })
        .ToListAsync();
}
```

### 14.4 복잡한 조합: 독립 조회 후 메모리에서 합산

여러 테이블의 데이터가 필요한 경우, 각각을 최적의 방식으로 독립 조회한 뒤 메모리에서 조합합니다. 하나의 거대한 쿼리로 모든 것을 가져오려고 하면 SQL이 복잡해지고 성능이 오히려 저하될 수 있습니다.

```csharp
public async Task<DashboardResponse> GetDashboardAsync(int userId)
{
    var recentOrders = await _context.Orders
        .AsNoTracking()
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.OrderDate)
        .Take(5)
        .Select(o => new { o.Id, o.OrderDate, o.Total, o.Status })
        .ToListAsync();

    var monthlySummary = await _context.Orders
        .AsNoTracking()
        .Where(o => o.UserId == userId
            && o.OrderDate >= DateTime.UtcNow.AddMonths(-6))
        .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
        .Select(g => new
        {
            Year = g.Key.Year, Month = g.Key.Month,
            Total = g.Sum(o => o.Total), Count = g.Count()
        })
        .ToListAsync();

    // 메모리에서 조합하여 응답 구성
    return new DashboardResponse
    {
        RecentOrders = recentOrders.Select(o => new OrderSummaryDto { ... }).ToList(),
        MonthlySummary = monthlySummary.Select(m => new MonthlySummaryDto { ... }).ToList()
    };
}
```

---

## 15. 쿼리 진단

### 15.1 ToQueryString

`ToQueryString()`은 IQueryable의 표현식 트리를 실제 실행될 SQL 문자열로 변환하여 반환하는 메서드입니다. 쿼리를 실행하지 않고 생성된 SQL만 확인할 수 있어 개발 중 디버깅에 유용합니다.

```csharp
var query = context.Products
    .AsNoTracking()
    .Where(p => p.Price > 100)
    .OrderBy(p => p.Name);

Console.WriteLine(query.ToQueryString());
```

### 15.2 TagWith

`TagWith(comment)`는 생성되는 SQL에 주석을 추가하는 메서드입니다. SQL Profiler나 로그에서 어떤 서비스·메서드에서 실행된 쿼리인지 추적할 수 있습니다.

```csharp
var orders = await context.Orders
    .TagWith("GetActiveOrders - OrderService")
    .AsNoTracking()
    .Where(o => o.IsActive)
    .ToListAsync();
// SQL 결과: /* GetActiveOrders - OrderService */ SELECT ...
```

### 15.3 로깅 설정

`appsettings.Development.json`에서 EF Core 커맨드 로그를 활성화하면 실행되는 모든 SQL을 확인할 수 있습니다.

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

`EnableSensitiveDataLogging()`을 추가하면 SQL 파라미터의 실제 값까지 로그에 포함됩니다. 운영 환경에서는 민감 데이터 노출에 주의하여 개발 환경에서만 활성화합니다.

---

## 16. 기타

### 16.1 Global Query Filter

`HasQueryFilter()`는 OnModelCreating에서 엔티티에 전역 필터 조건을 설정하는 메서드입니다. Soft Delete, 멀티테넌트 격리처럼 모든 쿼리에 공통 적용되어야 하는 조건을 정의하여 Where 누락 위험을 제거합니다. `IgnoreQueryFilters()`로 일시적 해제가 가능합니다.

### 16.2 Compiled Query

`EF.CompileAsyncQuery()`는 LINQ 표현식 트리를 한 번 컴파일하여 캐싱된 델리게이트로 변환하는 메서드입니다. 동일한 쿼리가 고빈도로 반복 실행되는 경우, 매번 발생하는 표현식 트리 → SQL 변환 비용을 제거합니다. static 필드에 캐싱하여 재사용합니다.

### 16.3 인덱스 설정

`HasIndex()`는 Fluent API에서 테이블 인덱스를 정의하는 메서드입니다. Where, OrderBy, Join에 사용되는 컬럼에 적절한 인덱스가 있는지 확인하는 것이 쿼리 최적화의 가장 근본적인 수단입니다. `IsUnique()`, `HasFilter()`, `IncludeProperties()` 등과 조합하여 유니크 인덱스, 필터 인덱스, 커버링 인덱스를 설정할 수 있습니다.
