# EF Core Functions (SQL Server / PostgreSQL)

---

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

## 2. 공통 함수 (프로바이더 무관)

### 2-1. Like

```csharp
EF.Functions.Like(string matchExpression, string pattern)
EF.Functions.Like(string matchExpression, string pattern, string escapeCharacter)
```

| 와일드카드 | 의미                 |
| ---------- | -------------------- |
| `%`        | 0개 이상 임의 문자   |
| `_`        | 정확히 1개 임의 문자 |

```csharp
// 포함 검색
.Where(x => EF.Functions.Like(x.ItemNo, $"%{keyword}%"))
// → ItemNo LIKE N'%ABC%'

// 전방 일치 (인덱스 활용 가능)
.Where(x => EF.Functions.Like(x.ItemNo, $"{keyword}%"))
// → ItemNo LIKE N'ABC%'

// 이스케이프 처리 (검색어에 % 또는 _ 가 포함된 경우)
.Where(x => EF.Functions.Like(x.Rate, @"50\%", @"\"))
// → Rate LIKE N'50\%' ESCAPE N'\'

// 선택적 필터 패턴 (실무 권장)
.Where(x =>
    string.IsNullOrEmpty(request.ItemNo) ||
    EF.Functions.Like(x.Items!.ItemNo, $"%{request.ItemNo}%"))
```

### 2-2. Collate

쿼리 시점에 콜레이션을 지정합니다. 대소문자 / 악센트 구분을 제어합니다.

```csharp
// SQL Server - 대소문자 구분 (CS)
.Where(x => EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CS_AS") == "John")

// SQL Server - 대소문자 무시 (CI)
.Where(x => EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CI_AS") == "john")

// PostgreSQL - 대소문자 무시
.Where(x => EF.Functions.Collate(x.Name, "und-x-icu") == "john")

// Like와 조합 (대소문자 무시 패턴 검색)
.Where(x => EF.Functions.Like(
    EF.Functions.Collate(x.Name, "SQL_Latin1_General_CP1_CI_AS"), "%john%"))
```

---

## 3. SQL Server 전용 함수

### 3-1. 전문 검색 (Full-Text Search)

> ⚠️ DB에 Full-Text Index가 미리 생성되어 있어야 합니다.

```sql
CREATE FULLTEXT INDEX ON Products(Description) KEY INDEX PK_Products;
```

#### Contains vs FreeText vs Like 결과 비교

| 검색어    | 데이터             | Like `%..%`  |  Contains   | FreeText |
| --------- | ------------------ | :----------: | :---------: | :------: |
| `EF Core` | "EF Core is great" |      ✅      |     ✅      |    ✅    |
| `Core EF` | "EF Core is great" | ❌ 순서 다름 |     ✅      |    ✅    |
| `ef core` | "EF Core is great" | ❌ 대소문자  |     ✅      |    ✅    |
| `EF Cor`  | "EF Core is great" |      ✅      | ❌ 단어단위 |    ❌    |
| `running` | "he runs daily"    |      ❌      |     ❌      | ✅ 어간  |

```csharp
// Contains - 단어 단위 검색
.Where(x => EF.Functions.Contains(x.Description, "EF Core"))
// → CONTAINS(Description, N'EF Core')

// AND / OR 조건
.Where(x => EF.Functions.Contains(x.Description, "\"EF\" AND \"Core\""))
.Where(x => EF.Functions.Contains(x.Description, "\"EF\" OR \"Entity\""))

// 전방 일치 단어 (접두어 검색)
.Where(x => EF.Functions.Contains(x.Description, "\"data*\""))
// → 'database', 'datastore' 등 매칭

// 근접 단어 (NEAR)
.Where(x => EF.Functions.Contains(x.Description, "\"EF\" NEAR \"Core\""))

// FreeText - 어간 변형 포함 자연어 검색
.Where(x => EF.Functions.FreeText(x.Description, "running"))
// → 'run', 'ran', 'runs', 'running' 모두 매칭
```

---

### 3-2. 날짜 차이 함수 (DateDiff)

```csharp
EF.Functions.DateDiffYear(start, end)       // → DATEDIFF(year, ...)
EF.Functions.DateDiffMonth(start, end)      // → DATEDIFF(month, ...)
EF.Functions.DateDiffWeek(start, end)       // → DATEDIFF(week, ...)
EF.Functions.DateDiffDay(start, end)        // → DATEDIFF(day, ...)
EF.Functions.DateDiffHour(start, end)       // → DATEDIFF(hour, ...)
EF.Functions.DateDiffMinute(start, end)     // → DATEDIFF(minute, ...)
EF.Functions.DateDiffSecond(start, end)     // → DATEDIFF(second, ...)
EF.Functions.DateDiffMillisecond(start, end)// → DATEDIFF(millisecond, ...)
EF.Functions.DateDiffMicrosecond(start, end)// → DATEDIFF(microsecond, ...)
EF.Functions.DateDiffNanosecond(start, end) // → DATEDIFF(nanosecond, ...)
```

```csharp
// 30일 이내 주문 조회
.Where(x => EF.Functions.DateDiffDay(x.OrderDate, DateTime.Now) <= 30)

// 경과 시간 계산 후 반환
.Select(x => new {
    x.OrderId,
    ElapsedDays  = EF.Functions.DateDiffDay(x.OrderDate, DateTime.Now),
    ElapsedHours = EF.Functions.DateDiffHour(x.CreatedAt, DateTime.UtcNow)
})
```

---

### 3-3. 날짜 생성 함수 (DateFromParts)

```csharp
EF.Functions.DateFromParts(year, month, day)
// → DATEFROMPARTS(@year, @month, @day)

EF.Functions.DateTime2FromParts(year, month, day, hour, minute, second, fractions, precision)
// → DATETIME2FROMPARTS(...)

EF.Functions.DateTimeFromParts(year, month, day, hour, minute, second, millisecond)
// → DATETIMEFROMPARTS(...)

EF.Functions.TimeFromParts(hour, minute, second, fractions, precision)
// → TIMEFROMPARTS(...)
```

```csharp
// 각 행의 연/월로 해당 월 1일 생성
.Select(x => new {
    x.Id,
    FirstDayOfMonth = EF.Functions.DateFromParts(x.Year, x.Month, 1)
})
```

---

### 3-4. 타임존 변환 (AtTimeZone)

```csharp
EF.Functions.AtTimeZone(dateTime, timeZone)
// → @dateTime AT TIME ZONE @timeZone
```

```csharp
.Select(x => new {
    x.Id,
    KstTime = EF.Functions.AtTimeZone(x.CreatedAt, "Korea Standard Time")
})
// → CreatedAt AT TIME ZONE N'Korea Standard Time'
```

---

### 3-5. 문자열 / 데이터 함수

```csharp
// CharIndex - 문자열 위치 반환 (1-based, 없으면 0)
.Where(x => EF.Functions.CharIndex("ABC", x.ItemNo) > 0)
// → WHERE CHARINDEX(N'ABC', ItemNo) > 0

// DataLength - 바이트 단위 길이 (후행 공백 포함, LEN과 다름)
.Select(x => EF.Functions.DataLength(x.Description))
// → SELECT DATALENGTH(Description)

// IsDate - 유효한 날짜 형식이면 1, 아니면 0
.Where(x => EF.Functions.IsDate(x.InputValue) == 1)
// → WHERE ISDATE(InputValue) = 1
```

---

### 3-6. 통계 집계 함수

```csharp
EF.Functions.StandardDeviationSample(values)    // → STDEV()
EF.Functions.StandardDeviationPopulation(values) // → STDEVP()
EF.Functions.VarianceSample(values)              // → VAR()
EF.Functions.VariancePopulation(values)          // → VARP()
```

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
```

---

## 4. PostgreSQL 전용 함수 (Npgsql)

패키지: `Npgsql.EntityFrameworkCore.PostgreSQL`

### 4-1. ILike (대소문자 무시 Like)

PostgreSQL은 `LIKE`가 대소문자를 구분합니다. `ILike`를 사용하면 대소문자 무시 검색이 가능합니다.

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

> SQL Server는 콜레이션으로 대소문자를 제어하지만, PostgreSQL은 `ILike`로 간단하게 처리합니다.

---

### 4-2. 정규식 (Regex)

PostgreSQL은 정규식 연산을 기본 지원하며, Npgsql EF Core 프로바이더는 .NET의 `Regex.IsMatch`를 자동으로 PostgreSQL 정규식 연산자로 변환합니다.

```csharp
// 대소문자 구분 정규식
.Where(x => Regex.IsMatch(x.ItemNo, @"^[A-Z]{3}-\d{4}$"))
// → ItemNo ~ '^[A-Z]{3}-\d{4}$'

// 대소문자 무시 (RegexOptions.IgnoreCase)
.Where(x => Regex.IsMatch(x.ItemNo, @"^abc", RegexOptions.IgnoreCase))
// → ItemNo ~* '^abc'

// 부정 매칭 (NOT ~)
.Where(x => !Regex.IsMatch(x.Name, @"\d+"))
// → Name !~ '\d+'
```

---

### 4-3. 문자열 함수

```csharp
// 문자열 뒤집기
.Select(x => EF.Functions.Reverse(x.Code))
// → SELECT reverse(Code)

// 문자열 → 배열 분리
.Select(x => EF.Functions.StringToArray(x.Tags, ","))
// → SELECT string_to_array(Tags, ',')

// null 문자 대체 포함
.Select(x => EF.Functions.StringToArray(x.Tags, ",", "NULL"))
// → SELECT string_to_array(Tags, ',', 'NULL')

// C# PadLeft/PadRight → lpad/rpad
.Select(x => x.Code.PadLeft(10, '0'))
// → SELECT lpad(Code, 10, '0')

// IndexOf → strpos (0-based 반환 주의: PostgreSQL은 1-based → EF가 -1 처리)
.Where(x => x.Name.IndexOf("ABC") >= 0)
// → WHERE strpos(Name, 'ABC') - 1 >= 0
```

---

### 4-4. 날짜 함수

PostgreSQL은 날짜 연산을 `INTERVAL`로 처리하기 때문에 SQL Server의 `DATEADD`와 다른 SQL이 생성됩니다.

```csharp
// AddDays → + INTERVAL '1 days'
.Select(x => x.OrderDate.AddDays(7))
// → OrderDate + INTERVAL '7 days'

// AddMonths, AddYears
.Select(x => x.OrderDate.AddMonths(1))
// → OrderDate + INTERVAL '1 months'

// 날짜 부분 추출 → date_part()
.Select(x => x.CreatedAt.Year)    // → date_part('year', CreatedAt)::INT
.Select(x => x.CreatedAt.Month)   // → date_part('month', CreatedAt)::INT
.Select(x => x.CreatedAt.Day)     // → date_part('day', CreatedAt)::INT

// TimeSpan 총합
.Select(g => EF.Functions.Sum(g.Select(x => x.Duration)))
// → sum(Duration)

// TimeSpan 평균
.Select(g => EF.Functions.Average(g.Select(x => x.Duration)))
// → avg(Duration)

// 날짜 생성
.Select(x => new DateTime(x.Year, x.Month, 1))
// → make_date(Year, Month, 1)

// 타임존 변환
.Select(x => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.CreatedAt, "Asia/Seoul"))
// → CreatedAt AT TIME ZONE 'Asia/Seoul'
```

---

### 4-5. JSON / JSONB 함수

```csharp
// JSONB 포함 여부 (jsonb @> jsonb)
.Where(x => EF.Functions.JsonContains(x.Properties, "{\"Status\": \"Active\"}"))
// → Properties @> '{"Status": "Active"}'

// 반대 방향 포함 여부 (jsonb <@ jsonb)
.Where(x => EF.Functions.JsonContained("{\"Status\": \"Active\"}", x.Properties))

// 특정 키 존재 여부 (jsonb ? key)
.Where(x => EF.Functions.JsonExists(x.Properties, "Status"))
// → Properties ? 'Status'

// 여러 키 중 하나라도 존재 (jsonb ?| keys)
.Where(x => EF.Functions.JsonExistAny(x.Properties, "Status", "Type"))
// → Properties ?| ARRAY['Status', 'Type']

// 모든 키 존재 (jsonb ?& keys)
.Where(x => EF.Functions.JsonExistAll(x.Properties, "Status", "Type"))
// → Properties ?& ARRAY['Status', 'Type']

// JSON 값 타입 확인
.Select(x => EF.Functions.JsonTypeof(x.Properties))
// → jsonb_typeof(Properties)
// 반환값: "object", "array", "string", "number", "boolean", "null"
```

---

### 4-6. 거리 / 유사도 함수

`btree_gist` 익스텐션이 필요합니다.

```csharp
// 날짜 거리 (정렬에 활용)
.OrderBy(x => EF.Functions.Distance(x.EventDate, targetDate))

// 타임스탬프 거리
.OrderBy(x => EF.Functions.Distance(x.CreatedAt, DateTime.UtcNow))
```

---

### 4-7. 행 값 비교 (Row Value Comparison)

페이지네이션에서 Keyset 방식 구현 시 유용합니다.

```csharp
// (CreatedAt, Id) > (lastCreatedAt, lastId) 형태의 커서 페이징
.Where(x => EF.Functions.GreaterThan(
    ValueTuple.Create(x.CreatedAt, x.Id),
    ValueTuple.Create(lastCreatedAt, lastId)))
// → (CreatedAt, Id) > (@lastCreatedAt, @lastId)

// 지원 메소드
EF.Functions.GreaterThan(a, b)           // a > b
EF.Functions.GreaterThanOrEqual(a, b)    // a >= b
EF.Functions.LessThan(a, b)              // a < b
EF.Functions.LessThanOrEqual(a, b)       // a <= b
```

---

## 5. SQL Server vs PostgreSQL 동일 목적, 다른 함수 비교

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

## 6. C# 표준 메소드 → SQL 자동 변환

`EF.Functions` 없이도 EF Core가 자동으로 SQL로 변환해주는 .NET 내장 메소드들입니다.

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

## 7. 실무 쿼리 패턴

### 7-1. 선택적 다중 필터

```csharp
.Where(x =>
    fromDate <= x.MovementDate && x.MovementDate <= toDate
    && (request.WarehouseKey == null   || x.WarehouseKey == request.WarehouseKey)
    && (string.IsNullOrEmpty(request.ItemNo) ||
        EF.Functions.Like(x.Items!.ItemNo, $"%{request.ItemNo}%")))
```

### 7-2. 1:N 관계 헤더 → 하위 컬렉션 필터 (Any)

```csharp
.Where(header =>
    string.IsNullOrEmpty(request.ItemNo) ||
    header.Details!.Any(d =>
        EF.Functions.Like(d.StockInouts!.Items!.ItemNo, $"%{request.ItemNo}%")))
// → WHERE EXISTS (SELECT 1 FROM Details d ... WHERE i.ItemNo LIKE N'%ABC%')
```

### 7-3. PostgreSQL Keyset 페이지네이션

```csharp
.Where(x => EF.Functions.GreaterThan(
    ValueTuple.Create(x.CreatedAt, x.Id),
    ValueTuple.Create(cursor.CreatedAt, cursor.Id)))
.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
.Take(pageSize)
// → WHERE (CreatedAt, Id) > (@cursorDate, @cursorId)
```

### 7-4. PostgreSQL JSONB 필터

```csharp
// Status가 "Active"인 항목 조회
.Where(x => EF.Functions.JsonContains(x.Properties, "{\"Status\":\"Active\"}"))

// 특정 키 존재 여부로 필터
.Where(x => EF.Functions.JsonExists(x.Properties, "ExpiredAt"))
```

---

## 8. 성능 고려사항

### Like 패턴별 인덱스 활용

```csharp
// ✅ Index Seek 가능
EF.Functions.Like(x.ItemNo, $"{keyword}%")   // 전방 일치

// ⚠️ Index Scan (대용량 주의)
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

## 9. 단위 테스트 주의사항

```csharp
// ❌ InMemory - EF.Functions.Like, DateDiff 등 미지원
optionsBuilder.UseInMemoryDatabase("TestDb");

// ✅ SQLite in-memory - Like 등 기본 함수 지원
optionsBuilder.UseSqlite("Data Source=:memory:");
```

SQL Server 전용 함수(`DateDiff`, `Contains`, `AtTimeZone`)나 PostgreSQL 전용 함수(`ILike`, `JsonContains`)는 SQLite로도 테스트 불가합니다. 해당 함수를 포함한 쿼리는 **실제 DB와 연결된 통합 테스트** 환경에서 검증해야 합니다.
