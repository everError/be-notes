### \#\# `DbSet.Local` 핵심 개념 💡

`DbSet<TEntity>.Local`은 EF Core의 `DbContext`가 **현재 추적하고 있는 엔터티만**을 담고 있는 **메모리 내(In-Memory) 컬렉션**입니다. 데이터베이스의 데이터가 아닌, 현재 컨텍스트가 알고 있는 데이터의 실시간 뷰입니다.

- **메모리 뷰**: `Local` 속성에 접근하는 것은 데이터베이스에 쿼리를 보내지 않습니다.
- **추적 엔터티만 포함**: `Added`, `Modified`, `Unchanged`, `Deleted` 상태의 엔터티만 포함합니다. `AsNoTracking()`으로 조회한 엔터티는 여기에 포함되지 않습니다.
- **실시간 동기화**: DB에서 데이터를 조회(`ToListAsync`, `FindAsync` 등)하거나 컨텍스트에 새 엔터티를 추가(`Add`, `Attach`)하면 `Local` 컬렉션에 즉시 반영됩니다.
- **요청 범위 생명주기 (ASP.NET Core 기준)**: `DbContext`는 일반적으로 단일 HTTP 요청 내에서만 유효하므로, `Local`의 데이터 역시 해당 요청이 끝나면 사라집니다. 여러 요청에 걸쳐 데이터를 공유하는 캐시가 아닙니다.

---

### \#\# 주요 활용 시나리오

`DbSet.Local`은 **`SaveChangesAsync`를 호출하기 전**, 하나의 작업 단위 내에서 **DB 데이터**와 **메모리에만 있는 새 데이터**를 모두 아우르는 복잡한 비즈니스 로직을 처리할 때 진정한 가치를 발휘합니다.

#### **시나리오 1: 복합 트랜잭션의 사전 검증**

여러 엔터티의 상태를 동시에 변경하고 검증해야 할 때 사용합니다.

- **예시**: 전자상거래 주문 처리 시, 여러 상품의 **재고를 메모리에서 미리 차감**해보고, 재고가 부족하지 않은지 모두 확인한 뒤 최종적으로 주문 생성과 재고 차감을 한 번의 `SaveChanges`로 커밋하는 경우.

#### **시나리오 2: 일괄 처리(Batch) 중 통합 데이터 검증**

이것이 `Local`의 가장 강력한 활용 사례입니다. 배치 데이터 처리 시, **DB 데이터**는 물론 **같은 배치 내의 다른 데이터**와의 중복 및 유효성을 동시에 검사할 수 있습니다.

- **예시**: '상품 일괄 등록' 시, 등록할 상품의 SKU가 **DB에 이미 있는지**와 **현재 등록 중인 목록 내에서 중복되는지**를 한 번에 확인하는 경우.

---

### \#\# 코드 예시: '상품 일괄 등록' 상세 구현

위 시나리오 2를 코드로 구현한 예시입니다.

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CommerceContext _db;

    public ProductsController(CommerceContext db)
    {
        _db = db;
    }

    [HttpPost("batch-register")]
    public async Task<IActionResult> BatchRegisterProducts(List<ProductDto> newProductsDto)
    {
        // 1. 등록할 SKU 목록으로 DB에 이미 존재하는 상품이 있는지 한 번에 조회하여 Local에 로드합니다.
        var incomingSkus = newProductsDto.Select(p => p.Sku).Distinct().ToList();
        await _db.Products.Where(p => incomingSkus.Contains(p.Sku)).LoadAsync();

        var productsToAdd = new List<Product>();
        foreach (var productDto in newProductsDto)
        {
            // 2. Local로 통합 검증을 수행합니다.
            //    - (1) DB에서 미리 로드한 기존 상품
            //    - (2) 이 루프의 이전 차수에서 메모리에 추가된 새 상품
            //    이 두 그룹 모두를 대상으로 SKU 중복을 한 번에 확인합니다.
            if (_db.Products.Local.Any(p => p.Sku == productDto.Sku))
            {
                return BadRequest($"SKU '{productDto.Sku}'가 중복됩니다.");
            }

            var newProduct = new Product
            {
                Sku = productDto.Sku,
                Name = productDto.Name,
                Price = productDto.Price,
                Stock = productDto.Stock
            };

            // 3. 검증 통과 시, 컨텍스트에 엔터티를 추가합니다.
            //    이 엔터티는 즉시 'Added' 상태로 Local 컬렉션에 포함되어,
            //    다음 루프의 검증 대상이 됩니다.
            _db.Products.Add(newProduct);
            productsToAdd.Add(newProduct);
        }

        // 4. 모든 검증이 성공적으로 끝나면, 단일 트랜잭션으로 모든 변경사항을 커밋합니다.
        await _db.SaveChangesAsync();

        return Ok(productsToAdd);
    }
}
```

---

### \#\# 최종 요약 및 주의사항

- **언제 사용해야 하는가?**
  하나의 트랜잭션 안에서 **DB에서 가져온 데이터**와 **아직 저장되지 않은 새 데이터**를 **혼합**하여 유효성을 검사하거나 비즈니스 로직을 수행해야 할 때 사용합니다.

- **주의사항**

  - `Local`은 여러 HTTP 요청 간에 유지되는 **글로벌 캐시가 절대 아닙니다.**
  - `AsNoTracking()`으로 조회한 엔터티는 `Local`에 포함되지 않으므로 주의해야 합니다.
  - `Local`은 `DbContext`의 상태를 반영할 뿐, 그 순간의 **데이터베이스 상태를 직접 반영하는 것은 아닙니다.**
