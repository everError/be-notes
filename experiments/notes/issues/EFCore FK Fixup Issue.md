## EF Core Navigation Fixup 동작 및 관계 구조 분석

### 🔍 현상 개요

- 엔티티 A는 외래 키(FK)로 엔티티 B를 참조한다 (예: `BId`, navigation: `B`).
- 동일한 B 인스턴스를 참조하는 A 인스턴스를 `DbContext`에 여러 개 추가할 경우,
  먼저 추가된 A의 `B` navigation 속성이 `null`로 바뀌는 현상이 발생한다.
- 이 동작은 SaveChanges 이전, 즉 **동일한 DbContext 및 단일 트랜잭션 범위 내에서 엔티티들이 아직 저장되지 않은 상태**에서도 발생하며,
  EF Core 내부의 관계 동기화(navigation fixup) 및 식별 해석(identity resolution) 로직이 작동함으로 인해 일어난다.

### 🔧 EF Core의 관계 해석 방식

EF Core는 모델을 구성할 때 다음 기준을 기반으로 엔티티 간 관계를 해석하고 내부적으로 "navigation fixup" 및 "identity resolution"을 수행한다:

1. A → B 관계가 **1:1 (One-to-One)** 으로 간주되면,
2. 하나의 B 인스턴스는 오직 하나의 A 인스턴스와만 연결될 수 있다고 해석된다.
3. 이후 동일한 B 인스턴스를 참조하는 A를 또 추가하면,
   EF Core는 기존의 A와 B 사이의 관계를 끊고 navigation을 `null`로 초기화한다.

### 💡 1:1 관계로 간주되는 예시 (속성 기반)

```csharp
public class A
{
    [ForeignKey(nameof(B))]
    public long? BId { get; set; }
    public B? B { get; set; }
}

public class B
{
    public A? A { get; set; } // 이 구조로 인해 EF Core는 1:1 관계로 인식
}
```

### 🔄 관계 방향과 구조 변경을 통한 다대일(1\:N) 관계 지정

EF Core가 다대일 관계로 인식하도록 하기 위해서는 B 쪽에 다수의 A를 가질 수 있음을 표현해야 한다:

#### B 엔티티 측을 다음과 같이 수정:

```csharp
public class B
{
    public ICollection<A> As { get; set; } = new();
}
```

#### 또는 Fluent API를 사용하여 명시적으로 관계 지정:

```csharp
modelBuilder.Entity<A>()
    .HasOne(a => a.B)
    .WithMany(b => b.As)
    .HasForeignKey(a => a.BId);
```

### 📌 정리

- EF Core는 관계 구조를 암묵적으로 해석하는 과정에서 1:1로 판단되면 관계 충돌 시 기존 navigation 값을 자동으로 null로 변경함.
- 이러한 동작은 SaveChanges 이전, 즉 메모리 상에서 엔티티가 추적되는 시점에서도 발생할 수 있다.
- 동일한 외래 키를 참조하는 여러 navigation이 존재할 수 있도록 하려면, 다대일(1\:N) 구조임을 B 측에 명확히 선언해야 한다.
- Attribute만 사용할 경우 이러한 관계의 방향성이 불분명할 수 있으며, 필요 시 Fluent API를 병행하는 것이 안정적이다.

---

### 📚 참고 정리

- EF Core는 navigation property와 foreign key 값이 정합성을 가지도록 내부적으로 일치 작업(fixup)을 자동 수행한다.
- 관계 방향과 multiplicity(1:1, 1\:N 등)의 해석은 navigation property의 구조에 따라 암묵적으로 결정된다.
- 모델을 설계할 때는 참조 방향성뿐 아니라 관계의 cardinality(수량 관계)를 명확히 표현하는 것이 중요하다.
- 단일 트랜잭션, 동일 DbContext 내에서 SaveChanges 이전에도 navigation fixup이 적극적으로 적용될 수 있음을 염두에 두어야 한다.
