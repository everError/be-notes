# EF Core에서 SQLite 기반 낙관적 동시성 제어 구현 가이드

이 문서는 SQLite와 EF Core를 사용하는 환경에서 낙관적 동시성 제어(Optimistic Concurrency Control)를 구현하는 방법을 정리한 실습 가이드입니다.

---

## ✅ 개요

SQLite는 SQL Server처럼 `ROWVERSION` 또는 `TIMESTAMP` 같은 자동 동시성 제어 기능이 없기 때문에, EF Core의 기능과 수동 전처리를 조합하여 낙관적 동시성 처리를 구현해야 합니다.

---

## 🧩 모델 설계

```csharp
public class Record
{
    public int Id { get; set; }
    public int Count { get; set; }

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();
}
```

### 🔎 설명

- `Version` 필드는 동시성 제어를 위한 식별자 역할을 하며, `Guid.NewGuid()`로 생성됩니다.
- `[ConcurrencyCheck]` 속성을 통해 EF Core가 SaveChanges 시 해당 속성을 비교하도록 만듭니다.
- 즉, 원래 버전과 현재 DB의 버전이 다르면 `DbUpdateConcurrencyException`이 발생합니다.

---

## ⚙️ Version 자동 업데이트 인터셉터

```csharp
public class ConcurrencyVersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetNewVersion(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetNewVersion(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetNewVersion(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<Record>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
```

### 🔎 설명

- SaveChanges 직전에 `Modified` 상태의 엔티티만 찾아 `Version`을 새로 할당합니다.
- 이 새 값은 SQL의 `SET` 절에 사용되며, `WHERE` 절에는 이전 값이 사용되어 동시성 충돌을 감지합니다.
- EF Core는 `OriginalValues["Version"]` 값을 `WHERE Version = @originalValue`로 사용합니다.

---

## 🔍 OriginalValues란?

- `OriginalValues`는 EF Core가 추적 중인 엔티티의 **변경 전 상태(snapshot)** 입니다.
- SaveChanges 시점에서 EF Core는 `OriginalValues`에 저장된 값과 DB의 값이 일치하는지 확인합니다.
- 만약 일치하지 않으면, 다른 트랜잭션이 값을 수정한 것으로 간주하고 `DbUpdateConcurrencyException`을 발생시킵니다.
- `OriginalValues`는 동시성 충돌 검사에서 `[ConcurrencyCheck]` 혹은 `IsConcurrencyToken`으로 설정된 속성에 한해 사용됩니다.
- 즉, 모든 속성을 검사하는 것이 아니라 동시성 토큰으로 명시된 항목만 비교 대상이 됩니다.

✅ 따라서 `OriginalValues.SetValues(databaseValues)`를 호출하면 EF Core는 다음 SaveChanges에서 최신 DB 값을 기준으로 동시성 토큰을 비교하므로, 충돌을 피할 수 있습니다.

---

## 🛡️ 컨트롤러 구현

### 1. `If-Match` 헤더를 이용한 명시적 충돌 감지

```csharp
[HttpPut("{id}/increment")]
public async Task<IActionResult> IncrementCount(
    int id, [FromHeader(Name = "If-Match")] Guid version)
{
    var record = await _db.Records.FirstOrDefaultAsync(r => r.Id == id);
    if (record == null)
        return NotFound();

    if (record.Version != version)
        return Conflict(new { message = "Concurrency conflict", currentVersion = record.Version });

    record.Count += 1;
    await _db.SaveChangesAsync();

    return Ok(new { message = "Success", newCount = record.Count, newVersion = record.Version });
}
```

### 🔎 설명

- 클라이언트가 마지막으로 본 `Version`을 헤더로 보내고, 서버는 이를 DB의 현재 값과 비교합니다.
- 일치하지 않으면 `409 Conflict` 반환 → 충돌 발생

---

### 2. 자동 재시도 API: 내부에서 충돌 감지 후 `Reload` + 재시도

```csharp
[HttpPut("{id}/increment-retry")]
public async Task<IActionResult> IncrementCountWithRetry(int id)
{
    const int maxRetry = 5;
    int attempt = 0;
    bool saved = false;

    while (!saved && attempt < maxRetry)
    {
        attempt++;

        var record = await _db.Records.FirstOrDefaultAsync(r => r.Id == id);
        if (record == null)
            return NotFound();

        record.Count += 1;

        try
        {
            await _db.SaveChangesAsync();
            saved = true;
            return Ok(new { message = $"Success after {attempt} attempts", record.Count, record.Version });
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in _db.ChangeTracker.Entries())
                await entry.ReloadAsync();
        }
    }

    return Conflict(new { message = "Failed to increment after retries" });
}
```

### 🔎 설명

- `DbUpdateConcurrencyException` 발생 시 `entry.ReloadAsync()`로 DB 상태를 다시 불러옴
- `ReloadAsync()`는 `GetDatabaseValuesAsync()`를 내부적으로 호출하여 현재 DB에 저장된 값을 반영합니다.
- 이후 `OriginalValues`가 새롭게 설정되므로 다음 Save 시에는 충돌 없이 적용됩니다.

---

## 🧠 공식 문서 기반 고급 구조 예시 + 설명

```csharp
bool saved = false;

while (!saved)
{
    try
    {
        await context.SaveChangesAsync();
        saved = true;
    }
    catch (DbUpdateConcurrencyException ex)
    {
        foreach (var entry in ex.Entries)
        {
            if (entry.Entity is Person)
            {
                var proposedValues = entry.CurrentValues; // 현재 내가 바꾸고 싶은 값
                var databaseValues = await entry.GetDatabaseValuesAsync(); // DB에 실제 저장된 최신 값

                foreach (var property in proposedValues.Properties)
                {
                    var proposedValue = proposedValues[property];
                    var databaseValue = databaseValues[property];

                    // 충돌 해결 전략: 어떤 값을 유지할지 결정
                    // 예: proposedValues[property] = databaseValue 또는 합산 값 등
                }

                // 이후 충돌 재발 방지를 위해 OriginalValues를 최신 값으로 설정
                entry.OriginalValues.SetValues(databaseValues);
            }
            else
            {
                throw new NotSupportedException(
                    "Don't know how to handle concurrency conflicts for " + entry.Metadata.Name);
            }
        }
    }
}
```

### 🔎 설명

- `GetDatabaseValuesAsync()`는 DB에 실제 저장된 최신 값(EntityEntry의 스냅샷)을 가져옵니다.
- `CurrentValues`는 메모리에 있는 현재 수정된 상태입니다.
- `OriginalValues`는 SaveChanges 시 EF가 비교 대상으로 삼는 값입니다.
- \*\*`entry.OriginalValues.SetValues(databaseValues)`\*\*를 호출해야 다음 Save에서 `Version`이 갱신된 DB 값과 일치하므로, 충돌 없이 저장됩니다.
- 반면 `entry.ReloadAsync()`는 `CurrentValues`까지 모두 DB값으로 덮어쓰기 때문에 사용 목적에 따라 구분해서 써야 합니다.

---

## 🔬 EF Core 내부 동작 정리

- `[ConcurrencyCheck]`가 붙은 필드는 변경 전 값이 `WHERE` 절에 자동 포함됨
- 인터셉터에서 `Version` 값을 바꿔도, 비교는 원래 값 기준으로 이루어짐
- 즉, `SET Version = 'B' WHERE Version = 'A'` 형태로 SQL이 구성됨
- `entry.ReloadAsync()`는 DB 값을 읽어와 `OriginalValues`와 `CurrentValues`를 동기화
- `entry.GetDatabaseValuesAsync()`는 `CurrentValues`와 분리된 DB 상태 스냅샷만 가져옴
- `OriginalValues`는 `[ConcurrencyCheck]` 또는 `IsConcurrencyToken` 속성에 한해서만 비교 수행 대상

---

## ✅ 정리

| 항목             | 설명                                                            |
| ---------------- | --------------------------------------------------------------- |
| DB               | SQLite (자동 RowVersion 없음)                                   |
| 동시성 제어 방식 | GUID 기반 수동 관리 + `[ConcurrencyCheck]`                      |
| 버전 갱신        | `SaveChangesInterceptor`에서 자동 설정                          |
| 충돌 처리        | 클라이언트 헤더 비교 or 서버 자동 재시도 or 수동 충돌 병합 처리 |

이 구조는 EF Core 기반으로 간단하지만 강력하게 동시성 충돌을 감지하고 회복할 수 있게 해줍니다.
