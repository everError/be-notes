# EF.Functions 완벽 가이드

## 1. 개요

`EF.Functions`는 LINQ 쿼리 안에서 데이터베이스 함수를 직접 호출할 수 있도록 EF Core가 제공하는 정적 프로퍼티입니다. C# 코드처럼 작성하지만 실제로는 **서버 사이드 SQL로 변환**되어 실행됩니다.

> ⚠️ EF 컨텍스트 밖(LINQ to Objects, 단위 테스트 등)에서 호출하면 `NotSupportedException`이 발생합니다. 메소드 본체는 `throw new InvalidOperationException()`만 있고, 실제 동작은 SQL 변환기가 담당합니다.

**패키지 구분**

| 패키지                                    | 클래스                           | 대상                 |
| ----------------------------------------- | -------------------------------- | -------------------- |
| `Microsoft.EntityFrameworkCore`           | `DbFunctionsExtensions`          | 공통 (Like, Collate) |
| `Microsoft.EntityFrameworkCore.SqlServer` | `SqlServerDbFunctionsExtensions` | SQL Server 전용      |
| `Npgsql.EntityFrameworkCore.PostgreSQL`   | `NpgsqlDbFunctionsExtensions`    | PostgreSQL 전용      |

---

## 2. EF.Functions를 써야 하는 경우

C# 표준 메소드(`string.Contains`, `<`, `>` 등)로 충분한 경우가 대부분이지만, 다음 상황에서는 `EF.Functions`가 필요합니다.

### 2-1. 와일드카드 패턴을 세밀하게 제어할 때

`string.Contains("keyword")`는 내부적으로 `LIKE '%keyword%'`로 변환되므로 결과는 동일합니다. 하지만 와일드카드를 직접 조합해야 하는 순간에는 `Like`가 필요합니다.

```csharp
// 전방 일치 → 인덱스 Seek 가능
EF.Functions.Like(x.ItemNo, $"{keyword}%")

// 복합 패턴: "A로 시작, 중간에 -, 숫자로 끝남"
EF.Functions.Like(x.Code, "A%-[0-9]")

// _ 와일드카드: 정확히 한 글자
EF.Functions.Like(x.Code, "AB_CD")
// → "AB1CD", "ABXCD" 매칭 / "ABCD", "AB12CD" 매칭 안 됨

// 검색어 자체에 %나 _가 포함된 경우 이스케이프
EF.Functions.Like(x.Rate, @"50\%", @"\")
// → "50%" 리터럴 검색. string.Contains("50%")로는 이스케이프 제어 불가
```

### 2-2. 전문 검색 (Full-Text Search)

`string.Contains`는 단순 문자열 매칭이므로 어간 분석, 단어 순서 무시, 근접 검색이 불가능합니다.

```csharp
// FreeText: "running" 검색 → run, ran, runs, running 모두 매칭 (어간 분석)
EF.Functions.FreeText(x.Description, "running")

// Contains: "EF Core" 검색 → "Core EF"도 매칭 (단어 순서 무관)
EF.Functions.Contains(x.Description, "\"EF\" AND \"Core\"")

// 접두어 검색: "data*" → database, datastore, dataset 매칭
EF.Functions.Contains(x.Description, "\"data*\"")
```

### 2-3. 대소문자 구분을 쿼리 시점에 제어할 때

DB 기본 콜레이션과 다르게 동작시켜야 하는 경우입니다.

```csharp
// SQL Server: 기본 CI인데 이 쿼리만 대소문자 구분
EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CS_AS")

// PostgreSQL: LIKE는 기본 CS → 대소문자 무시하려면 ILike
EF.Functions.ILike(x.Name, "%john%")
```

### 2-4. 두 날짜의 차이값 자체가 필요할 때

단순 크기 비교(`x.OrderDate >= startDate`)는 `<`, `>`로 충분합니다. 차이를 **숫자로 꺼내야** 할 때 `DateDiff`가 필요합니다.

```csharp
// "주문 후 며칠 지났는지"를 컬럼으로 반환
.Select(x => new {
    x.OrderId,
    ElapsedDays = EF.Functions.DateDiffDay(x.OrderDate, DateTime.Now)
})
// 결과 예시: { OrderId = 42, ElapsedDays = 15 }
```

### 2-5. C# 표준에 대응하는 문법이 없는 DB 고유 기능

| 기능                                   | 왜 C# 표준으로 안 되는가                                                                       |
| -------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `AtTimeZone`                           | SQL Server의 `AT TIME ZONE` 구문을 직접 매핑. C# `TimeZoneInfo`는 SQL Server 쿼리로 변환 안 됨 |
| `StandardDeviationSample` 등 통계 함수 | LINQ에 표준 편차/분산 집계가 없음                                                              |
| `JsonContains`, `JsonExists` 등        | PostgreSQL JSONB 연산자(`@>`, `?`)에 대응하는 C# 문법 없음                                     |
| `GreaterThan(ValueTuple)`              | 복합 컬럼 비교 `(A, B) > (C, D)`를 C# 표준으로 표현 불가                                       |

---

## 3. 공통 함수 (프로바이더 무관)

### 3-1. Like

SQL의 `LIKE` 연산자를 직접 사용합니다. `string.Contains`와 달리 와일드카드 패턴(`%`, `_`)을 직접 제어할 수 있고, 이스케이프 문자를 지정할 수 있습니다.

```csharp
EF.Functions.Like(string matchExpression, string pattern)
EF.Functions.Like(string matchExpression, string pattern, string escapeCharacter)
```

| 와일드카드 | 의미                 | 예시                                           |
| ---------- | -------------------- | ---------------------------------------------- |
| `%`        | 0개 이상 임의 문자   | `"A%"` → "A", "AB", "ABC" 모두 매칭            |
| `_`        | 정확히 1개 임의 문자 | `"A_C"` → "ABC" 매칭, "AC"나 "ABBC" 매칭 안 됨 |

```csharp
// 포함 검색 (string.Contains("ABC")와 결과 동일)
.Where(x => EF.Functions.Like(x.ItemNo, $"%{keyword}%"))
// → ItemNo LIKE N'%ABC%'

// 전방 일치 (인덱스 활용 가능)
.Where(x => EF.Functions.Like(x.ItemNo, $"{keyword}%"))
// → ItemNo LIKE N'ABC%'

// 이스케이프 처리 (검색어에 % 또는 _ 가 포함된 경우)
.Where(x => EF.Functions.Like(x.Rate, @"50\%", @"\"))
// → Rate LIKE N'50\%' ESCAPE N'\'
// "50%"라는 문자열 자체를 찾음. 이스케이프 없으면 %가 와일드카드로 동작

// 선택적 필터 패턴 (실무 권장)
.Where(x =>
    string.IsNullOrEmpty(request.ItemNo) ||
    EF.Functions.Like(x.Items!.ItemNo, $"%{request.ItemNo}%"))
```

### 3-2. Collate

쿼리 시점에 콜레이션을 지정합니다. 콜레이션은 문자열의 비교/정렬 규칙으로, 대소문자 구분(CS/CI), 악센트 구분(AS/AI) 등을 제어합니다. DB 기본 콜레이션과 다른 규칙을 특정 쿼리에만 적용하고 싶을 때 사용합니다.

```csharp
// SQL Server - 대소문자 구분 (CS = Case Sensitive)
// DB 기본이 CI(대소문자 무시)여도 이 쿼리만 구분
.Where(x => EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CS_AS") == "John")
// → "John" 매칭 / "john", "JOHN" 매칭 안 됨

// SQL Server - 대소문자 무시 (CI = Case Insensitive)
// DB 기본이 CS여도 이 쿼리만 무시
.Where(x => EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CI_AS") == "john")
// → "John", "JOHN", "john" 모두 매칭

// PostgreSQL - 대소문자 무시
.Where(x => EF.Functions.Collate(x.Name, "und-x-icu") == "john")

// Like와 조합 (대소문자 무시 패턴 검색)
.Where(x => EF.Functions.Like(
    EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CI_AS"), "%john%"))
```

---

## 4. SQL Server 전용 함수

### 4-1. 전문 검색 (Full-Text Search)

> ⚠️ DB에 Full-Text Index가 미리 생성되어 있어야 합니다.

```sql
CREATE FULLTEXT INDEX ON Products(Description) KEY INDEX PK_Products;
```

**Contains**: 단어 단위로 정확히 매칭합니다. 단어 순서는 무관하고, AND/OR/NEAR 같은 논리 연산을 지원합니다. 부분 문자열("EF Cor")은 매칭하지 않습니다.

**FreeText**: 자연어 검색입니다. 내부적으로 단어의 어간(stem)을 분석하여 변형된 형태까지 매칭합니다. 예를 들어 "running"을 검색하면 "run", "ran", "runs"도 결과에 포함됩니다.

**Contains vs FreeText vs Like 결과 비교**

| 검색어    | 데이터             | Like `%..%`  | Contains    | FreeText |
| --------- | ------------------ | ------------ | ----------- | -------- |
| `EF Core` | "EF Core is great" | ✅           | ✅          | ✅       |
| `Core EF` | "EF Core is great" | ❌ 순서 다름 | ✅          | ✅       |
| `ef core` | "EF Core is great" | ❌ 대소문자  | ✅          | ✅       |
| `EF Cor`  | "EF Core is great" | ✅           | ❌ 단어단위 | ❌       |
| `running` | "he runs daily"    | ❌           | ❌          | ✅ 어간  |

```csharp
// Contains - 단어 단위 검색
.Where(x => EF.Functions.Contains(x.Description, "EF Core"))
// → CONTAINS(Description, N'EF Core')

// AND / OR 조건
.Where(x => EF.Functions.Contains(x.Description, "\"EF\" AND \"Core\""))
// → Description에 "EF"와 "Core" 두 단어가 모두 포함된 행
.Where(x => EF.Functions.Contains(x.Description, "\"EF\" OR \"Entity\""))
// → Description에 "EF" 또는 "Entity" 중 하나라도 있는 행

// 전방 일치 단어 (접두어 검색)
.Where(x => EF.Functions.Contains(x.Description, "\"data*\""))
// → 'database', 'datastore', 'dataset' 등 "data"로 시작하는 단어 매칭

// 근접 단어 (NEAR)
.Where(x => EF.Functions.Contains(x.Description, "\"EF\" NEAR \"Core\""))
// → "EF"와 "Core"가 서로 가까이 위치한 행

// FreeText - 어간 변형 포함 자연어 검색
.Where(x => EF.Functions.FreeText(x.Description, "running"))
// → 'run', 'ran', 'runs', 'running' 모두 매칭
```

---

### 4-2. 날짜 차이 함수 (DateDiff)

두 날짜 사이의 차이를 지정한 단위(년, 월, 일 등)의 **정수**로 반환합니다. SQL Server의 `DATEDIFF` 함수에 직접 매핑됩니다.

> ⚠️ `DATEDIFF`는 경계(boundary) 횟수를 세는 방식입니다. 예를 들어 `DateDiffYear(2023-12-31, 2024-01-01)`은 실제로 하루 차이지만, 연도 경계를 1번 넘었으므로 **1**을 반환합니다.

```csharp
EF.Functions.DateDiffYear(start, end)       // 연도 경계 횟수 → DATEDIFF(year, ...)
EF.Functions.DateDiffMonth(start, end)      // 월 경계 횟수   → DATEDIFF(month, ...)
EF.Functions.DateDiffWeek(start, end)       // 주 경계 횟수   → DATEDIFF(week, ...)
EF.Functions.DateDiffDay(start, end)        // 일 경계 횟수   → DATEDIFF(day, ...)
EF.Functions.DateDiffHour(start, end)       // 시간 경계 횟수 → DATEDIFF(hour, ...)
EF.Functions.DateDiffMinute(start, end)     // 분 경계 횟수   → DATEDIFF(minute, ...)
EF.Functions.DateDiffSecond(start, end)     // 초 경계 횟수   → DATEDIFF(second, ...)
EF.Functions.DateDiffMillisecond(start, end)// 밀리초 경계    → DATEDIFF(millisecond, ...)
EF.Functions.DateDiffMicrosecond(start, end)// 마이크로초 경계 → DATEDIFF(microsecond, ...)
EF.Functions.DateDiffNanosecond(start, end) // 나노초 경계    → DATEDIFF(nanosecond, ...)
```

```csharp
// 30일 이내 주문 조회
.Where(x => EF.Functions.DateDiffDay(x.OrderDate, DateTime.Now) <= 30)
// OrderDate가 2024-01-01이고 Now가 2024-01-20이면 → 19 → 30 이하이므로 포함

// 경과 시간 계산 후 반환
.Select(x => new {
    x.OrderId,
    ElapsedDays  = EF.Functions.DateDiffDay(x.OrderDate, DateTime.Now),
    ElapsedHours = EF.Functions.DateDiffHour(x.CreatedAt, DateTime.UtcNow)
})
// 결과 예시: { OrderId = 42, ElapsedDays = 15, ElapsedHours = 360 }
```

---

### 4-3. 날짜 생성 함수 (DateFromParts)

개별 숫자 컬럼(연, 월, 일 등)으로부터 날짜/시간 값을 조립합니다. DB에 연/월/일이 따로 저장된 경우 유용합니다.

```csharp
EF.Functions.DateFromParts(year, month, day)
// → DATEFROMPARTS(@year, @month, @day)
// 예: DateFromParts(2024, 3, 15) → 2024-03-15

EF.Functions.DateTime2FromParts(year, month, day, hour, minute, second, fractions, precision)
// → DATETIME2FROMPARTS(...)
// 예: DateTime2FromParts(2024, 3, 15, 14, 30, 0, 0, 7) → 2024-03-15 14:30:00.0000000

EF.Functions.DateTimeFromParts(year, month, day, hour, minute, second, millisecond)
// → DATETIMEFROMPARTS(...)
// 예: DateTimeFromParts(2024, 3, 15, 14, 30, 0, 0) → 2024-03-15 14:30:00.000

EF.Functions.TimeFromParts(hour, minute, second, fractions, precision)
// → TIMEFROMPARTS(...)
// 예: TimeFromParts(14, 30, 0, 0, 7) → 14:30:00.0000000
```

```csharp
// 각 행의 연/월로 해당 월 1일 생성
.Select(x => new {
    x.Id,
    FirstDayOfMonth = EF.Functions.DateFromParts(x.Year, x.Month, 1)
})
// Year=2024, Month=3인 행 → FirstDayOfMonth = 2024-03-01
```

---

### 4-4. 타임존 변환 (AtTimeZone)

SQL Server의 `AT TIME ZONE` 구문을 사용하여 서버 사이드에서 타임존을 변환합니다. UTC로 저장된 시간을 특정 타임존 기준으로 표시할 때 유용합니다.

> C#의 `TimeZoneInfo.ConvertTime`은 SQL Server 쿼리로 변환되지 않으므로, 서버 사이드에서 타임존 변환이 필요하면 `AtTimeZone`을 써야 합니다.

```csharp
EF.Functions.AtTimeZone(dateTime, timeZone)
// → @dateTime AT TIME ZONE @timeZone
```

```csharp
.Select(x => new {
    x.Id,
    KstTime = EF.Functions.AtTimeZone(x.CreatedAt, "Korea Standard Time")
})
// CreatedAt이 2024-01-15 00:00:00 UTC라면
// → KstTime = 2024-01-15 09:00:00 +09:00
```

---

### 4-5. 문자열 / 데이터 함수

```csharp
// CharIndex - 문자열 위치 반환 (1-based, 없으면 0)
// C#의 IndexOf와 유사하지만 1부터 시작. 못 찾으면 0 반환
.Where(x => EF.Functions.CharIndex("ABC", x.ItemNo) > 0)
// → WHERE CHARINDEX(N'ABC', ItemNo) > 0
// ItemNo가 "XABC123"이면 → CHARINDEX 결과 2 → 조건 충족

// DataLength - 바이트 단위 길이 (후행 공백 포함)
// LEN()은 후행 공백 제거 후 문자 수, DataLength는 바이트 수 그대로
.Select(x => EF.Functions.DataLength(x.Description))
// → SELECT DATALENGTH(Description)
// "ABC"(nvarchar) → 6 (유니코드 1문자 = 2바이트)
// "ABC "(nvarchar, 후행 공백 1개) → 8

// IsDate - 유효한 날짜 형식이면 1, 아니면 0
// 문자열이 날짜로 변환 가능한지 검증
.Where(x => EF.Functions.IsDate(x.InputValue) == 1)
// → WHERE ISDATE(InputValue) = 1
// "2024-01-15" → 1 (유효) / "not-a-date" → 0 (무효)
```

---

### 4-6. 통계 집계 함수

LINQ 표준에는 `Average`, `Sum`, `Min`, `Max`, `Count`만 있고 표준 편차나 분산은 없습니다. 이 함수들은 SQL Server의 통계 집계 함수에 직접 매핑됩니다.

**각 함수의 의미:**

| 함수                          | SQL        | 의미             | 설명                                                                                                 |
| ----------------------------- | ---------- | ---------------- | ---------------------------------------------------------------------------------------------------- |
| `StandardDeviationSample`     | `STDEV()`  | 표본 표준 편차   | 데이터가 전체 모집단의 일부(표본)일 때 사용. 값들이 평균에서 얼마나 퍼져 있는지를 나타냄. N-1로 나눔 |
| `StandardDeviationPopulation` | `STDEVP()` | 모집단 표준 편차 | 데이터가 전체 모집단일 때 사용. N으로 나눔                                                           |
| `VarianceSample`              | `VAR()`    | 표본 분산        | 표준 편차의 제곱. 데이터의 산포도를 나타냄. N-1로 나눔                                               |
| `VariancePopulation`          | `VARP()`   | 모집단 분산      | 전체 모집단의 분산. N으로 나눔                                                                       |

> **예시**: 판매액이 [100, 200, 300]인 경우
>
> - 평균 = 200
> - 표본 분산 (`VAR`) = ((100-200)² + (200-200)² + (300-200)²) / (3-1) = 10000
> - 표본 표준 편차 (`STDEV`) = √10000 = 100 → "판매액이 평균 200에서 ±100 정도 퍼져 있다"
> - 모집단 분산 (`VARP`) = 20000 / 3 ≈ 6666.67
> - 모집단 표준 편차 (`STDEVP`) = √6666.67 ≈ 81.65

```csharp
var stats = await _context.Sales
    .GroupBy(x => x.ProductId)
    .Select(g => new {
        ProductId = g.Key,
        Avg    = g.Average(x => x.Amount),
        StdDev = EF.Functions.StandardDeviationSample(g.Select(x => x.Amount)),
        Var    = EF.Functions.VarianceSample(g.Select(x => x.Amount))
    })
    .ToListAsync();
// → SELECT ProductId, AVG(Amount), STDEV(Amount), VAR(Amount)
//   FROM Sales GROUP BY ProductId
//
// 결과 예시:
// ProductId=1, Avg=200, StdDev=100, Var=10000
// → "상품1의 평균 판매액은 200이고, 편차가 100이므로 대략 100~300 범위에 분포"
//
// ProductId=2, Avg=200, StdDev=10, Var=100
// → "상품2도 평균 200이지만 편차가 10이므로 190~210에 집중 → 매출이 안정적"
```

---

## 5. PostgreSQL 전용 함수 (Npgsql)

패키지: `Npgsql.EntityFrameworkCore.PostgreSQL`

### 5-1. ILike (대소문자 무시 Like)

PostgreSQL의 `LIKE`는 대소문자를 구분합니다. `ILike`는 PostgreSQL의 `ILIKE` 연산자에 매핑되어 대소문자를 무시합니다.

> SQL Server는 기본 콜레이션이 CI(대소문자 무시)인 경우가 많아 `LIKE`만으로도 대소문자 무시 검색이 되지만, PostgreSQL은 항상 CS(구분)이므로 `ILike`가 필요합니다.

```csharp
// LIKE는 대소문자 구분
.Where(x => EF.Functions.Like(x.Name, "%john%"))
// → Name LIKE '%john%'  → 'John', 'JOHN' 매칭 안 됨

// ILike는 대소문자 무시
.Where(x => EF.Functions.ILike(x.Name, "%john%"))
// → Name ILIKE '%john%'  → 'John', 'JOHN', 'john' 모두 매칭

// 이스케이프 처리
.Where(x => EF.Functions.ILike(x.Name, @"50\%", @"\"))
```

---

### 5-2. 정규식 (Regex)

PostgreSQL은 정규식 연산을 기본 지원하며, Npgsql EF Core 프로바이더는 .NET의 `Regex.IsMatch`를 자동으로 PostgreSQL 정규식 연산자(`~`, `~*`)로 변환합니다.

```csharp
// 대소문자 구분 정규식
.Where(x => Regex.IsMatch(x.ItemNo, @"^[A-Z]{3}-\d{4}$"))
// → ItemNo ~ '^[A-Z]{3}-\d{4}$'
// "ABC-1234" 매칭 / "abc-1234", "AB-123" 매칭 안 됨

// 대소문자 무시 (RegexOptions.IgnoreCase)
.Where(x => Regex.IsMatch(x.ItemNo, @"^abc", RegexOptions.IgnoreCase))
// → ItemNo ~* '^abc'
// "ABC123", "abc123", "Abc123" 모두 매칭

// 부정 매칭 (NOT ~)
.Where(x => !Regex.IsMatch(x.Name, @"\d+"))
// → Name !~ '\d+'
// 숫자가 하나도 없는 행만 반환
```

---

### 5-3. 문자열 함수

```csharp
// 문자열 뒤집기
.Select(x => EF.Functions.Reverse(x.Code))
// → SELECT reverse(Code)
// "ABC" → "CBA"

// 문자열 → 배열 분리
.Select(x => EF.Functions.StringToArray(x.Tags, ","))
// → SELECT string_to_array(Tags, ',')
// "red,green,blue" → {"red", "green", "blue"}

// null 문자 대체 포함
.Select(x => EF.Functions.StringToArray(x.Tags, ",", "NULL"))
// → SELECT string_to_array(Tags, ',', 'NULL')
// "red,NULL,blue" → {"red", NULL, "blue"} (문자열 "NULL"이 실제 NULL로 변환)

// C# PadLeft/PadRight → lpad/rpad (자동 변환)
.Select(x => x.Code.PadLeft(10, '0'))
// → SELECT lpad(Code, 10, '0')
// "ABC" → "0000000ABC"

// IndexOf → strpos (자동 변환, 0-based)
.Where(x => x.Name.IndexOf("ABC") >= 0)
// → WHERE strpos(Name, 'ABC') - 1 >= 0
// PostgreSQL strpos는 1-based이지만 EF가 -1 처리하여 C#과 동일하게 동작
```

---

### 5-4. 날짜 함수

PostgreSQL은 날짜 연산을 `INTERVAL`로 처리하기 때문에 SQL Server의 `DATEADD`와 다른 SQL이 생성됩니다. 대부분 C# 표준 메소드가 자동 변환되지만, 결과 SQL이 다르다는 점을 알아두면 디버깅에 유용합니다.

```csharp
// AddDays → + INTERVAL '1 days'
.Select(x => x.OrderDate.AddDays(7))
// → OrderDate + INTERVAL '7 days'
// 2024-01-01 → 2024-01-08

// AddMonths, AddYears
.Select(x => x.OrderDate.AddMonths(1))
// → OrderDate + INTERVAL '1 months'
// 2024-01-31 → 2024-02-29 (자동으로 말일 처리)

// 날짜 부분 추출 → date_part()
.Select(x => x.CreatedAt.Year)    // → date_part('year', CreatedAt)::INT
.Select(x => x.CreatedAt.Month)   // → date_part('month', CreatedAt)::INT
.Select(x => x.CreatedAt.Day)     // → date_part('day', CreatedAt)::INT

// TimeSpan 총합
.Select(g => EF.Functions.Sum(g.Select(x => x.Duration)))
// → sum(Duration)
// Duration이 [01:00:00, 02:30:00, 00:30:00]이면 → 04:00:00

// TimeSpan 평균
.Select(g => EF.Functions.Average(g.Select(x => x.Duration)))
// → avg(Duration)
// 위 데이터면 → 01:20:00

// 날짜 생성
.Select(x => new DateTime(x.Year, x.Month, 1))
// → make_date(Year, Month, 1)
// Year=2024, Month=3 → 2024-03-01

// 타임존 변환
.Select(x => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.CreatedAt, "Asia/Seoul"))
// → CreatedAt AT TIME ZONE 'Asia/Seoul'
// 2024-01-15 00:00:00 UTC → 2024-01-15 09:00:00 KST
```

---

### 5-5. JSON / JSONB 함수

PostgreSQL의 JSONB 연산자(`@>`, `<@`, `?`, `?|`, `?&`)를 LINQ에서 사용할 수 있게 합니다.

```csharp
// JSONB 포함 여부 (jsonb @> jsonb)
// Properties에 {"Status": "Active"} 키-값 쌍이 포함되어 있는지 검사
.Where(x => EF.Functions.JsonContains(x.Properties, "{\"Status\": \"Active\"}"))
// → Properties @> '{"Status": "Active"}'
// {"Status":"Active","Name":"A"} → ✅ / {"Status":"Inactive"} → ❌

// 반대 방향 포함 여부 (jsonb <@ jsonb)
// 주어진 JSON이 Properties에 포함되는지 (방향 반대)
.Where(x => EF.Functions.JsonContained("{\"Status\": \"Active\"}", x.Properties))

// 특정 키 존재 여부 (jsonb ? key)
.Where(x => EF.Functions.JsonExists(x.Properties, "Status"))
// → Properties ? 'Status'
// {"Status":"Active"} → ✅ / {"Name":"A"} → ❌ (값은 상관없이 키만 검사)

// 여러 키 중 하나라도 존재 (jsonb ?| keys)
.Where(x => EF.Functions.JsonExistAny(x.Properties, "Status", "Type"))
// → Properties ?| ARRAY['Status', 'Type']
// {"Status":"A"} → ✅ / {"Other":"B"} → ❌

// 모든 키 존재 (jsonb ?& keys)
.Where(x => EF.Functions.JsonExistAll(x.Properties, "Status", "Type"))
// → Properties ?& ARRAY['Status', 'Type']
// {"Status":"A","Type":"B"} → ✅ / {"Status":"A"} → ❌ (Type 키 없음)

// JSON 값 타입 확인
.Select(x => EF.Functions.JsonTypeof(x.Properties))
// → jsonb_typeof(Properties)
// {"a":1} → "object" / [1,2] → "array" / "hello" → "string" / 42 → "number"
```

---

### 5-6. 거리 / 유사도 함수

`btree_gist` 익스텐션이 필요합니다. 두 값 사이의 "거리"를 계산하여 가장 가까운 값 순으로 정렬할 때 유용합니다.

```csharp
// 날짜 거리 (타겟 날짜에 가장 가까운 이벤트 순 정렬)
.OrderBy(x => EF.Functions.Distance(x.EventDate, targetDate))
// targetDate가 2024-03-15일 때:
// 2024-03-14(거리1), 2024-03-20(거리5), 2024-03-10(거리5) 순

// 타임스탬프 거리
.OrderBy(x => EF.Functions.Distance(x.CreatedAt, DateTime.UtcNow))
```

---

### 5-7. 행 값 비교 (Row Value Comparison)

페이지네이션에서 Keyset(커서) 방식 구현 시 유용합니다. 여러 컬럼을 하나의 튜플로 묶어서 비교하면, 복합 정렬 키 기반의 페이지네이션을 단일 조건으로 표현할 수 있습니다.

> Offset 방식 (`SKIP N`)은 N이 클수록 느려지지만, Keyset 방식은 인덱스를 타므로 일정한 성능을 유지합니다.

```csharp
// (CreatedAt, Id) > (lastCreatedAt, lastId) 형태의 커서 페이징
.Where(x => EF.Functions.GreaterThan(
    ValueTuple.Create(x.CreatedAt, x.Id),
    ValueTuple.Create(lastCreatedAt, lastId)))
// → (CreatedAt, Id) > (@lastCreatedAt, @lastId)
//
// 단순 AND 조합과 다름:
// WHERE CreatedAt > @last OR (CreatedAt = @last AND Id > @lastId)
// 위를 한 줄로 표현한 것. DB 엔진이 더 효율적으로 처리 가능

// 지원 메소드
EF.Functions.GreaterThan(a, b)           // a > b
EF.Functions.GreaterThanOrEqual(a, b)    // a >= b
EF.Functions.LessThan(a, b)             // a < b
EF.Functions.LessThanOrEqual(a, b)      // a <= b
```

---

## 6. SQL Server vs PostgreSQL 동일 목적, 다른 함수 비교

| 목적               | SQL Server                        | PostgreSQL                                            |
| ------------------ | --------------------------------- | ----------------------------------------------------- |
| 대소문자 무시 검색 | `Collate(..., "CI_AS")`           | `ILike(...)`                                          |
| 정규식 검색        | 없음 (기본 미지원)                | `Regex.IsMatch(...)` → `~`                            |
| 날짜 차이          | `EF.Functions.DateDiffDay(a, b)`  | `(a - b).Days` or `date_part`                         |
| 날짜 더하기        | `dateTime.AddDays(n)` → `DATEADD` | `dateTime.AddDays(n)` → `+ INTERVAL`                  |
| 타임존 변환        | `AtTimeZone(dt, tz)`              | `TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dt, tz)`  |
| 문자열 뒤집기      | 없음 (기본 미지원)                | `EF.Functions.Reverse(s)`                             |
| 전문 검색          | `Contains(...)` / `FreeText(...)` | `EF.Functions.ToTsVector` / `ToTsQuery` (별도 페이지) |
| JSON 포함 검색     | 없음 (기본 미지원)                | `EF.Functions.JsonContains(...)`                      |

---

## 7. C# 표준 메소드 → SQL 자동 변환

`EF.Functions` 없이도 EF Core가 자동으로 SQL로 변환해주는 .NET 내장 메소드들입니다. 아래 메소드들은 별도 함수 호출 없이 LINQ에서 바로 사용하면 됩니다.

### 문자열

```csharp
x.Name.Contains("keyword")       // SS: LIKE '%keyword%'  / PG: LIKE '%keyword%'
x.Name.StartsWith("keyword")     // SS: LIKE 'keyword%'   / PG: LIKE 'keyword%'
x.Name.EndsWith("keyword")       // SS: LIKE '%keyword'   / PG: LIKE '%keyword'
x.Name.Length                    // SS: LEN()             / PG: length()
x.Name.ToUpper()                 // SS: UPPER()           / PG: upper()
x.Name.ToLower()                 // SS: LOWER()           / PG: lower()
x.Name.Trim()                    // SS: LTRIM(RTRIM())    / PG: btrim()
x.Name.TrimStart()               // SS: LTRIM()           / PG: ltrim()
x.Name.TrimEnd()                 // SS: RTRIM()           / PG: rtrim()
x.Name.Replace("a", "b")        // SS: REPLACE()         / PG: replace()
x.Name.Substring(0, 3)          // SS: SUBSTRING()       / PG: substr()
x.Name.IndexOf("a")             // SS: CHARINDEX()-1     / PG: strpos()-1
x.Desc ?? "없음"                 // SS: COALESCE()        / PG: COALESCE()
string.IsNullOrEmpty(x.Name)    // SS: IS NULL OR =''    / PG: IS NULL OR =''
string.Join(", ", group.Select(x => x.Name))  // SS: STRING_AGG / PG: string_agg
```

### 날짜

```csharp
DateTime.Now           // SS: GETDATE()       / PG: now()::timestamp
DateTime.UtcNow        // SS: GETUTCDATE()    / PG: now()
DateTime.Today         // SS: CONVERT(date, GETDATE()) / PG: date_trunc('day', now())
x.Date.Year            // SS: DATEPART(year)  / PG: date_part('year')::INT
x.Date.Month           // SS: DATEPART(month) / PG: date_part('month')::INT
x.Date.Day             // SS: DATEPART(day)   / PG: date_part('day')::INT
x.Date.AddDays(7)      // SS: DATEADD(day,7)  / PG: + INTERVAL '7 days'
x.Date.Date            // SS: CONVERT(date)   / PG: date_trunc('day')
```

### 수학

```csharp
Math.Abs(x.Value)      // → ABS()
Math.Ceiling(x.Value)  // → CEILING() / ceil()
Math.Floor(x.Value)    // → FLOOR()
Math.Round(x.Value)    // → ROUND()
Math.Pow(x.Value, 2)   // → POWER()
Math.Sqrt(x.Value)     // → SQRT()
Math.Log(x.Value)      // → LOG()
Math.Sin(x.Angle)      // → SIN()
Math.Cos(x.Angle)      // → COS()
```

### 타입 변환

```csharp
Convert.ToInt32(x.StrVal)    // SS: CONVERT(int, ...)   / PG: CAST(... AS integer)
Convert.ToString(x.IntVal)   // SS: CONVERT(nvarchar)   / PG: CAST(... AS text)
Convert.ToDecimal(x.Val)     // SS: CONVERT(decimal)    / PG: CAST(... AS numeric)
```

---

## 8. 실무 쿼리 패턴

### 8-1. 선택적 다중 필터

검색 조건이 입력된 경우에만 필터를 적용하는 패턴입니다. `null`이거나 빈 문자열이면 해당 조건을 건너뜁니다.

```csharp
.Where(x =>
    fromDate <= x.MovementDate && x.MovementDate <= toDate
    && (request.WarehouseKey == null   || x.WarehouseKey == request.WarehouseKey)
    && (string.IsNullOrEmpty(request.ItemNo) ||
        EF.Functions.Like(x.Items!.ItemNo, $"%{request.ItemNo}%")))
```

### 8-2. 1:N 관계 헤더 → 하위 컬렉션 필터 (Any)

```csharp
.Where(header =>
    string.IsNullOrEmpty(request.ItemNo) ||
    header.Details!.Any(d =>
        EF.Functions.Like(d.StockInouts!.Items!.ItemNo, $"%{request.ItemNo}%")))
// → WHERE EXISTS (SELECT 1 FROM Details d ... WHERE i.ItemNo LIKE N'%ABC%')
```

### 8-3. PostgreSQL Keyset 페이지네이션

```csharp
.Where(x => EF.Functions.GreaterThan(
    ValueTuple.Create(x.CreatedAt, x.Id),
    ValueTuple.Create(cursor.CreatedAt, cursor.Id)))
.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
.Take(pageSize)
// → WHERE (CreatedAt, Id) > (@cursorDate, @cursorId)
```

### 8-4. PostgreSQL JSONB 필터

```csharp
// Status가 "Active"인 항목 조회
.Where(x => EF.Functions.JsonContains(x.Properties, "{\"Status\":\"Active\"}"))

// 특정 키 존재 여부로 필터
.Where(x => EF.Functions.JsonExists(x.Properties, "ExpiredAt"))
```

---

## 9. 성능 고려사항

### Like 패턴별 인덱스 활용

```csharp
// ✅ Index Seek 가능 - B-Tree 인덱스를 정상적으로 활용
EF.Functions.Like(x.ItemNo, $"{keyword}%")   // 전방 일치

// ⚠️ Index Scan (대용량 주의) - 인덱스가 있어도 전체를 훑어야 함
EF.Functions.Like(x.ItemNo, $"%{keyword}%")  // 양방향
EF.Functions.Like(x.ItemNo, $"%{keyword}")   // 후방 일치
```

PostgreSQL에서 양방향 Like를 자주 쓴다면 **pg_trgm + GIN 인덱스** 조합을 고려하세요.

```sql
CREATE EXTENSION pg_trgm;
CREATE INDEX idx_items_itemno_trgm ON items USING GIN (itemno gin_trgm_ops);
-- 이후 LIKE '%keyword%' 도 인덱스 활용 가능
```

---

## 10. 단위 테스트 주의사항

```csharp
// ❌ InMemory - EF.Functions.Like, DateDiff 등 미지원
optionsBuilder.UseInMemoryDatabase("TestDb");

// ✅ SQLite in-memory - Like 등 기본 함수 지원
optionsBuilder.UseSqlite("Data Source=:memory:");
```

SQL Server 전용 함수(`DateDiff`, `Contains`, `AtTimeZone`)나 PostgreSQL 전용 함수(`ILike`, `JsonContains`)는 SQLite로도 테스트 불가합니다. 해당 함수를 포함한 쿼리는 **실제 DB와 연결된 통합 테스트** 환경에서 검증해야 합니다.
