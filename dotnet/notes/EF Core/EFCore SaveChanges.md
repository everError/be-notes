## EF Core: `SaveChanges()` 메서드

### 1\. 개요

`SaveChanges()`는 Entity Framework Core에서 `DbContext`가 추적하고 있는 메모리 상의 변경 사항들을 실제 데이터베이스에 영구적으로 적용(Persist)하는 메서드입니다. `Add()`, `Update()`, `Remove()` 등의 메서드는 단지 `DbContext`의 변경 추적기(Change Tracker)에 엔터티의 상태를 등록할 뿐이며, `SaveChanges()`가 호출되어야 비로소 데이터베이스와 동기화가 이루어집니다.

---

### 2\. 주요 역할 및 책임

- **변경 사항 감지 (Change Detection)**
  `DbContext`에 연결된 모든 엔터티 인스턴스의 상태를 검사하여 `Added`(추가됨), `Modified`(수정됨), `Deleted`(삭제됨) 상태인 엔터티들을 식별합니다. `Unchanged` 상태인 엔터티는 무시됩니다.

- **SQL 생성 (SQL Generation)**
  감지된 엔터티의 상태에 따라 실행할 `INSERT`, `UPDATE`, `DELETE` SQL 구문을 생성합니다. 예를 들어 `Added` 상태의 엔터티는 `INSERT` 구문으로, `Modified` 상태는 `UPDATE` 구문으로 변환됩니다.

- **트랜잭션 관리 (Transaction Management)**
  생성된 모든 SQL 구문들을 단일 트랜잭션(Transaction)으로 감싸서 실행합니다. 이로 인해 `SaveChanges()` 내의 모든 작업은 원자성(Atomicity)을 보장받습니다. 즉, 모든 SQL이 성공하거나 하나라도 실패하면 전체가 롤백(Rollback)됩니다.

---

### 3\. 동작 순서

`SaveChanges()` 메서드가 호출되면 내부적으로 다음 순서에 따라 동작합니다.

1.  `DbContext.SaveChanges()` 메서드가 호출됩니다.
2.  EF Core의 변경 추적기(`ChangeTracker`)가 추적 중인 모든 엔터티를 스캔하여 상태가 변경된(`Added`, `Modified`, `Deleted`) 엔터티를 찾습니다.
3.  각각의 변경 사항에 해당하는 `INSERT`, `UPDATE`, `DELETE` SQL 커맨드를 생성합니다.
4.  EF Core가 데이터베이스 트랜잭션을 시작합니다. (단, 사용자가 `BeginTransaction()`으로 명시적 트랜잭션을 이미 시작한 경우는 예외)
5.  생성된 SQL 커맨드들을 데이터베이스로 전송하여 트랜잭션 내에서 실행합니다. 이 시점에 구성된 로깅 공급자(Logging Provider)가 SQL 로그를 출력합니다.
6.  모든 커맨드가 성공적으로 실행되면 트랜잭션을 \*\*커밋(Commit)\*\*하여 변경 사항을 데이터베이스에 최종 확정합니다.
7.  커맨드 실행 중 하나라도 실패하면 트랜잭션을 \*\*롤백(Rollback)\*\*하고 `DbUpdateException` 예외를 발생시킵니다.
8.  작업이 성공적으로 완료되면, 변경이 적용된 엔터티들의 상태를 `Unchanged`로 업데이트합니다.

---

### 4\. 반환 값

`SaveChanges()` 메서드는 데이터베이스에 성공적으로 저장된 엔터티의 수를 정수(`int`) 타입으로 반환합니다.

```csharp
// public virtual int SaveChanges()

int numberOfAffectedEntries = _context.SaveChanges();
```

이 값은 `Added`, `Modified`, `Deleted` 상태였다가 데이터베이스에 성공적으로 반영된 엔터티들의 총 개수입니다.

---

### 5\. `SaveChangesAsync()` (비동기 버전)

`SaveChanges()`의 비동기 버전으로, I/O 작업을 논블로킹(Non-blocking) 방식으로 처리합니다. 데이터베이스와의 통신이 완료될 때까지 스레드를 차단하지 않으므로, 특히 웹 애플리케이션과 같이 동시성과 응답성이 중요한 환경에서 사용이 강력히 권장됩니다.

```csharp
// public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)

int numberOfAffectedEntries = await _context.SaveChangesAsync();
```

---

### 6\. 트랜잭션과의 관계

- **암시적 트랜잭션 (Implicit Transaction)**
  별도의 트랜잭션 설정이 없을 경우, `SaveChanges()`는 호출될 때마다 자체적으로 트랜잭션을 생성하고 작업을 완료한 뒤 커밋합니다. 따라서 `SaveChanges()` 호출 한 번은 그 자체로 하나의 원자적 단위입니다.

- **명시적 트랜잭션 (Explicit Transaction)**
  개발자가 `_context.Database.BeginTransaction()`을 사용하여 직접 트랜잭션을 시작한 경우, `SaveChanges()`는 새로운 트랜잭션을 생성하지 않고 기존에 진행 중인 명시적 트랜잭션에 참여합니다. 이 경우 트랜잭션의 최종 확정(`Commit`) 및 롤백(`Rollback`)은 개발자가 직접 `transaction.Commit()` 또는 `transaction.Rollback()`을 호출하여 제어해야 합니다. 이는 여러 `SaveChanges()` 호출을 하나의 논리적 작업 단위로 묶을 때 사용됩니다.
