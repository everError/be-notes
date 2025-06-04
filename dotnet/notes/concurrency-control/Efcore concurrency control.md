# EF Core 동시성 문제 및 해결 전략 개요

이 문서는 EF Core를 기반으로 API에서 특정 Row의 수량(또는 카운트) 값을 증가시키는 작업을 수행할 때 발생할 수 있는 \*\*Race Condition(경쟁 조건)\*\*과 **동시성 문제**에 대해 설명하고, 다양한 해결 전략을 정리한 가이드입니다.

---

## ✅ 문제 상황 예시

수량을 +1 하는 단순한 API를 여러 클라이언트가 동시에 호출할 경우 다음과 같은 문제가 발생할 수 있습니다.

### ❗ 문제점 1: 갱신 손실 (Lost Update)

- 두 클라이언트가 동시에 DB에서 `5`를 읽음
- 둘 다 `+1` 계산 후 `6`을 저장함
- 실제로는 `+2`가 되어야 하는데 `+1`만 반영됨

### ❗ 문제점 2: 동시성 충돌 예외

- EF Core가 내부적으로 동시성 토큰을 사용 중일 경우 `DbUpdateConcurrencyException` 발생 가능

---

## ⚙️ 해결 전략

### 🔹 1. 데이터베이스 수준에서 직접 증가시키기

```csharp
await db.Database.ExecuteSqlInterpolatedAsync(
    $"UPDATE Counters SET Value = Value + 1 WHERE Id = {id}");
```

- ✅ 충돌 없음
- ✅ 빠름
- ❌ ChangeTracker의 상태 관리와 무관함

---

### 🔹 2. 비관적 락(Pessimistic Lock) 사용

```csharp
var row = await db.Counters
    .FromSqlInterpolated($"SELECT * FROM Counters WHERE Id = {id} FOR UPDATE")
    .FirstAsync();

row.Value++;
await db.SaveChangesAsync();
```

- ✅ 충돌 방지
- ❌ SQLite 등 일부 DB는 `FOR UPDATE` 미지원
- ✅ SQL Server, PostgreSQL 등에서는 사용 가능

---

### 🔹 3. 동시성 토큰 (RowVersion) 사용

```csharp
public class Counter
{
    public int Id { get; set; }
    public int Value { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

- 충돌 발생 시 `DbUpdateConcurrencyException`
- 해결책: 재시도 로직 적용 (`retry on fail`)

---

### 🔹 4. RedLock 기반 분산 락 사용

- Redis 기반 분산 락으로 요청을 순차 처리
- 특히 MSA, 다중 인스턴스 환경에서 유용

예: `RedLock.net`, `StackExchange.Redis`, `Medallion.Threading.Redis` 등 활용

```csharp
using (var redLock = await redlockFactory.CreateLockAsync("counter-lock", TimeSpan.FromSeconds(5)))
{
    if (redLock.IsAcquired)
    {
        // 안전하게 DB 업데이트 수행
    }
}
```

---

## ✅ 전략별 비교 요약

| 전략          | 충돌 방지        | ChangeTracker 사용 | 분산 환경 적합          | 난이도 |
| ------------- | ---------------- | ------------------ | ----------------------- | ------ |
| SQL 직접 증가 | ✅               | ❌                 | ✅                      | 하     |
| 비관적 락     | ✅               | ✅                 | ❌ (단일 인스턴스 권장) | 중     |
| 동시성 토큰   | ⚠️ (재시도 필요) | ✅                 | ⚠️                      | 중     |
| RedLock       | ✅               | ✅                 | ✅                      | 상     |

---

## 📌 결론 및 권장 흐름

1. 단순한 로컬 테스트: SQL 직접 증가 또는 RowVersion 사용
2. 단일 인스턴스 운영 환경: 비관적 락 또는 SQL 직접 증가
3. MSA 구조 + 다중 인스턴스: RedLock 기반 락으로 요청 직렬화 처리
