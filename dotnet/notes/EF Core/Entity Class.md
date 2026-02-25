# EF Core Entity 클래스 작성

---

## PK (Primary Key)

### 단일 PK

```csharp
[PrimaryKey(nameof(SystemCode))]
[Comment("시스템 정보")]
public class Systems
{
    [Required, StringLength(40)]
    [Comment("시스템 식별 코드")]
    public string SystemCode { get; set; } = default!;
}
```

### 자동 증가 PK

```csharp
[PrimaryKey(nameof(LotKey))]
[Comment("LOT 정보")]
public class Lots
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Comment("LOT 식별 번호")]
    public long LotKey { get; private set; }
}
```

### 복합 PK

```csharp
[PrimaryKey(nameof(MaterialMovementKey), nameof(StockInoutKey))]
[Comment("자재이동 상세")]
public class MaterialMovementDetails
{
    [Comment("자재이동 식별 번호")]
    public long MaterialMovementKey { get; set; }

    [Comment("수불 식별 번호")]
    public long StockInoutKey { get; set; }
}
```

---

## 문자열 및 값 제약

### 문자열 길이

```csharp
[Required, StringLength(40)]
[Comment("품목 코드")]
public string ItemCode { get; set; } = default!;

// MaxLength도 가능하지만 StringLength 사용을 권장 (일관성)
```

### 숫자 정밀도

```csharp
[Precision(19, 5)]
[Comment("수량")]
public decimal Quantity { get; set; }
```

### 값 범위

```csharp
[Range(0, double.MaxValue)]
[Comment("수량")]
public decimal Quantity { get; set; }

// Precision과 함께 사용
[Precision(19, 5)]
[Range(0, double.MaxValue)]
[Comment("수량")]
public decimal Quantity { get; set; }
```

---

## 인덱스

### 단일 인덱스

```csharp
[Index(nameof(ItemCode))]
[Comment("품목 정보")]
public class Items
{
    [Required, StringLength(40)]
    [Comment("품목 코드")]
    public string ItemCode { get; set; } = default!;
}
```

### 복합 인덱스

```csharp
[Index(nameof(ItemKey), nameof(WarehouseKey))]
[Comment("LOT 정보")]
public class Lots
{
    [Comment("품목 식별 번호")]
    public long ItemKey { get; set; }

    [Comment("창고 식별 번호")]
    public long WarehouseKey { get; set; }
}
```

### 유니크 인덱스

```csharp
[Index(nameof(ItemCode), IsUnique = true)]
[Comment("품목 정보")]
public class Items
{
    [Required, StringLength(40)]
    [Comment("품목 코드")]
    public string ItemCode { get; set; } = default!;
}
```

### 복합 유니크 인덱스

```csharp
[Index(nameof(ItemKey), nameof(WarehouseKey), IsUnique = true)]
[Comment("LOT 정보")]
public class Lots
{
    [Comment("품목 식별 번호")]
    public long ItemKey { get; set; }

    [Comment("창고 식별 번호")]
    public long WarehouseKey { get; set; }
}
```

---

## FK 및 관계 선언

### 단일 FK (N:1)

가장 일반적인 패턴입니다. FK 프로퍼티에 `[ForeignKey]`를 붙이고 네비게이션 이름을 지정합니다.

```csharp
[Comment("LOT 정보")]
public class Lots
{
    [ForeignKey(nameof(Items))]
    [Comment("품목 식별 번호")]
    public long ItemKey { get; set; }
    public Items Items { get; set; } = default!;

    [ForeignKey(nameof(Warehouses))]
    [Comment("창고 식별 번호")]
    public long WarehouseKey { get; set; }
    public Warehouses Warehouses { get; set; } = default!;
}
```

### Nullable FK (N:1, 선택적 관계)

```csharp
[ForeignKey(nameof(Lots))]
[Comment("LOT 식별 번호")]
public long? LotKey { get; set; }
public Lots? Lots { get; set; }
```

### 복합 FK

네비게이션 프로퍼티에 `[ForeignKey]`를 붙이고 콤마로 구분합니다.

```csharp
[Comment("자재이동 식별 번호")]
public long? MaterialMovementKey { get; set; }

[Comment("수불 식별 번호")]
public long? StockInoutKey { get; set; }

[ForeignKey(nameof(MaterialMovementKey) + "," + nameof(StockInoutKey))]
public MaterialMovementDetails? MaterialMovementDetails { get; set; }
```

> **주의**: 복합 FK에서 각 프로퍼티에 개별적으로 `[ForeignKey]`를 붙이면 오류가 발생합니다. 반드시 네비게이션에 콤마로 묶어서 지정하세요.

---

## 관계 유형별 선언

### 1:N (One-to-Many)

```csharp
// 1 쪽 (부모)
[Comment("품목 정보")]
public class Items
{
    [Key]
    [Comment("품목 식별 번호")]
    public long ItemKey { get; set; }

    // 컬렉션 네비게이션
    public ICollection<Lots> Lots { get; set; }
}

// N 쪽 (자식)
[Comment("LOT 정보")]
public class Lots
{
    [ForeignKey(nameof(Items))]
    [Comment("품목 식별 번호")]
    public long ItemKey { get; set; }
    public Items Items { get; set; } = default!;
}
```

### 1:1 (One-to-One)

부모 쪽에 단일 네비게이션을 선언하면 EF Core가 1:1로 인식하고 FK에 Unique 인덱스를 자동 생성합니다.

```csharp
// 부모
[Comment("생산투입 LOT")]
public class ProductionInputLots
{
    [Key]
    [Comment("생산투입 LOT 식별 번호")]
    public long ProductionInputLotKey { get; set; }

    // 단일 네비게이션 → 1:1로 인식 → FK에 Unique 인덱스 자동 생성
    public LabelInfos? LabelInfos { get; set; }
}

// 자식
[Comment("라벨 정보")]
public class LabelInfos
{
    [ForeignKey(nameof(ProductionInputLots))]
    [Comment("생산투입 LOT 식별 번호")]
    public long? ProductionInputLotKey { get; set; }
    public ProductionInputLots? ProductionInputLots { get; set; }
}
```

> **1:N인데 Unique 인덱스를 원하지 않는 경우**: 부모 쪽에 `ICollection`으로 선언하거나, 부모에 네비게이션을 두지 않고 Fluent API로 `HasMany`를 지정합니다.
>
> ```csharp
> // Fluent API — 부모에 컬렉션 네비게이션 없이 1:N 관계 설정
> modelBuilder.Entity<ProductionInputLots>()
>     .HasMany<LabelInfos>()
>     .WithOne(x => x.ProductionInputLots)
>     .HasForeignKey(x => x.ProductionInputLotKey);
> ```

### N:M (Many-to-Many)

중간 테이블 엔티티를 명시적으로 작성합니다.

```csharp
// 중간 테이블
[PrimaryKey(nameof(ItemKey), nameof(WarehouseKey))]
[Comment("품목-창고 매핑")]
public class ItemWarehouses
{
    [ForeignKey(nameof(Items))]
    [Comment("품목 식별 번호")]
    public long ItemKey { get; set; }
    public Items Items { get; set; } = default!;

    [ForeignKey(nameof(Warehouses))]
    [Comment("창고 식별 번호")]
    public long WarehouseKey { get; set; }
    public Warehouses Warehouses { get; set; } = default!;
}

// 양쪽 엔티티에 컬렉션 네비게이션
public class Items
{
    public ICollection<ItemWarehouses> ItemWarehouses { get; set; }
}

public class Warehouses
{
    public ICollection<ItemWarehouses> ItemWarehouses { get; set; }
}
```

---

## 자주 사용하는 Attribute 요약

| Attribute             | 용도                    | 예시                                                    |
| --------------------- | ----------------------- | ------------------------------------------------------- |
| `[PrimaryKey]`        | PK 지정 (클래스 레벨)   | `[PrimaryKey(nameof(Key))]`                             |
| `[Key]`               | PK 지정 (프로퍼티 레벨) | `[Key]`                                                 |
| `[DatabaseGenerated]` | 자동 생성 전략          | `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` |
| `[Required]`          | NOT NULL                | `[Required]`                                            |
| `[StringLength]`      | 문자열 최대 길이        | `[StringLength(40)]`                                    |
| `[Precision]`         | 숫자 정밀도             | `[Precision(19, 5)]`                                    |
| `[Range]`             | 값 범위 제약            | `[Range(0, double.MaxValue)]`                           |
| `[Comment]`           | 테이블/컬럼 주석        | `[Comment("설명")]`                                     |
| `[Index]`             | 인덱스 (클래스 레벨)    | `[Index(nameof(Code), IsUnique = true)]`                |
| `[ForeignKey]`        | FK 지정                 | `[ForeignKey(nameof(Items))]`                           |
| `[NotMapped]`         | 매핑 제외               | `[NotMapped]`                                           |
| `[Column]`            | 컬럼명/타입 지정        | `[Column("col_name", TypeName = "jsonb")]`              |
| `[Table]`             | 테이블명 지정           | `[Table("tbl_items")]`                                  |

---

## 잘못된 예시

```csharp
// ❌ Comment 누락, StringLength 누락, Required 누락
public class Systems
{
    public string SystemCode { get; set; }
    public string SystemName { get; set; }
}

// ❌ 복합 FK를 각 프로퍼티에 개별 선언
[ForeignKey(nameof(MaterialMovementDetails))]
public long? MaterialMovementKey { get; set; }
[ForeignKey(nameof(MaterialMovementDetails))]
public long? StockInoutKey { get; set; }
public MaterialMovementDetails? MaterialMovementDetails { get; set; }

// ✅ 복합 FK는 네비게이션에 콤마로 지정
public long? MaterialMovementKey { get; set; }
public long? StockInoutKey { get; set; }
[ForeignKey(nameof(MaterialMovementKey) + "," + nameof(StockInoutKey))]
public MaterialMovementDetails? MaterialMovementDetails { get; set; }
```

## 참고 문서

- [EF Core Docs](https://learn.microsoft.com/ef/core/)
- [dotnet ef CLI Reference](https://learn.microsoft.com/ef/core/cli/dotnet)
