## EF Core에서 알아야 할 `IQueryable` vs `IEnumerable` 차이 정리

Entity Framework Core(EF Core)에서 LINQ를 사용할 때 `IQueryable`과 `IEnumerable`의 차이를 이해하는 것은 성능과 동작에 큰 영향을 미칩니다.

---

### ✅ 공통점

* 둘 다 LINQ 쿼리에서 사용할 수 있는 컬렉션 인터페이스입니다.
* `foreach`, `Select`, `Where` 등과 함께 사용할 수 있습니다.

---

### 📌 핵심 차이점

| 항목             | `IQueryable`                      | `IEnumerable`                   |
| -------------- | --------------------------------- | ------------------------------- |
| 정의 위치          | `System.Linq`                     | `System.Collections`            |
| 실행 시점          | **지연 실행 (Deferred Execution)**    | **즉시 실행 (Immediate Execution)** |
| 쿼리 수행 위치       | **DB에서 실행됨 (서버측)**                | **메모리 내에서 실행됨 (클라이언트측)**        |
| 성능             | **성능 최적화 가능 (필터링, 정렬 등 DB에서 처리)** | **쿼리 이후 처리로 비효율 발생 가능**         |
| 용도             | **EF Core 쿼리 작성 시 주로 사용**         | **메모리에 로드된 데이터 처리 시 사용**        |
| LINQ 메서드 적용 방식 | `Expression<Func<T>>`로 번역 가능      | `Func<T>` 기반으로 실행됨              |
| 문법 지원          | 제한적 (C# 컴파일러 문법 일부 사용 불가)         | C# 문법 전체 사용 가능                  |

---

### ⚙️ 문법/표현식 차이 (컴파일러 관점)

`IQueryable`은 내부적으로 **Expression Tree**로 변환되어 DB 쿼리로 번역되기 때문에, 아래와 같은 **C# 고급 문법**을 자유롭게 사용할 수 없습니다:

| 문법 요소                    | `IQueryable`                          | `IEnumerable` |
| ------------------------ | ------------------------------------- | ------------- |
| null 조건 연산자 (`?.`, `??`) | ❌ 일부 제한 있음                            | ✅ 가능          |
| null-forgiving 연산자 (`!`) | ⚠️ **무시됨 (Expression Tree에 반영되지 않음)** | ✅ 컴파일러에서 적용됨  |
| `nameof`, `typeof`       | ❌ Expression Tree로 변환 불가              | ✅ 가능          |
| `is`, `as`, 패턴 매칭        | ❌ 복잡한 구문은 제한됨                         | ✅ 가능          |
| 지역 변수/메서드 호출             | ❌ 사용 제한 많음                            | ✅ 자유롭게 사용 가능  |

> `!`(null-forgiving 연산자)는 `IQueryable`에서는 단순히 컴파일러에서 null 관련 경고를 억제하는 용도로만 사용되며, 런타임 Expression에는 **아무 영향도 주지 않습니다**. 따라서 DB 쿼리에는 영향을 미치지 않고, 대부분 **무시**됩니다.

**예시:**

```csharp
// IQueryable에서는 다음 구문이 실패할 수 있음
var users = context.Users.Where(u => u.Name?.StartsWith("A") == true); // ⚠️ 실패 가능

// IEnumerable에서는 가능
var users = context.Users
                   .AsEnumerable()
                   .Where(u => u.Name?.StartsWith("A") == true);
```

---

### 🔍 예제 코드 비교

```csharp
// IQueryable 예제 (DB에서 필터링)
var users = context.Users
                   .Where(u => u.Age > 30) // SQL WHERE 절로 변환됨
                   .ToList();

// IEnumerable 예제 (메모리에서 필터링)
var users = context.Users
                   .AsEnumerable() // DB 쿼리 후 메모리로 로드됨
                   .Where(u => u.Age > 30) // LINQ가 메모리 내에서 필터링 수행
                   .ToList();
```

---

### ⚠️ 주의사항

* `AsEnumerable()` 또는 `ToList()` 호출 후 LINQ 메서드를 사용하는 경우, **DB가 아닌 메모리에서 처리**되어 성능 저하 유발 가능
* 특히 `Where`, `OrderBy`, `Select` 등의 필터링/변환 로직은 **가능한 한 `IQueryable` 상태에서 작성**해야 최적화된 SQL로 변환됨

---

### 🧠 정리

| 상황                          | 추천 타입         |
| --------------------------- | ------------- |
| DB에서 조건 필터, 정렬, 페이지네이션 등 수행 | `IQueryable`  |
| 이미 로드된 컬렉션에 대해 후처리          | `IEnumerable` |

> 🔑 **EF Core에서는 가능한 한 `IQueryable`을 유지하며 DB 레벨에서 처리하도록 작성하는 것이 성능에 유리**합니다.

---

### 📚 참고 메서드

* `.AsQueryable()`: IEnumerable을 IQueryable로 변환
* `.AsEnumerable()`: IQueryable을 IEnumerable로 변환 (DB 실행 발생 후 메모리 전환)
* `.ToList()`: 즉시 실행, 결과를 메모리에 리스트로 저장

---

### ✅ 실전 팁

* `.FirstOrDefault()`를 `IQueryable`로 실행하면 SQL 쿼리에서 LIMIT 1처럼 작동
* `.ToList().FirstOrDefault()`는 전체 로드를 한 뒤 첫 항목을 선택하므로 비효율적임

```csharp
// 효율적인 방법 (DB에서 LIMIT 1 처리)
var user = context.Users.FirstOrDefault(u => u.Email == "abc@site.com");

// 비효율적인 방법 (전체 로드 후 첫 번째 항목 선택)
var user = context.Users.ToList().FirstOrDefault(u => u.Email == "abc@site.com");
```

---

이러한 차이를 이해하고 적절히 사용하는 것이 EF Core에서 고성능의 쿼리를 작성하는 핵심입니다.
