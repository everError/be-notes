# EF Core + SQLite 마이그레이션 (.NET 8 기준)

이 문서는 .NET 8 기반 ASP.NET Core 애플리케이션에서 Entity Framework Core를 사용하여 SQLite와 마이그레이션 기능을 연동하고 실제 애플리케이션 실행 흐름과 연결하는 과정을 정리한 가이드입니다.

---

## 📦 주요 환경 및 도구

- .NET 8
- Entity Framework Core 8
- SQLite (파일 기반 경량 DB)
- `dotnet ef` CLI 도구

---

## 🧱 기본 구조

```bash
data-service/
├── Models/
│   └── Record.cs
├── Data/
│   └── AppDbContext.cs
├── Controllers/
│   └── RecordsController.cs
├── Program.cs
└── appsettings.json
```

---

## 📌 마이그레이션 개념 요약

EF Core의 마이그레이션은 코드 기반으로 DB 스키마(테이블 구조 등)를 정의하고, 이를 DB에 적용하는 방식입니다.

- `migrations add` → 마이그레이션 스냅샷 코드 생성
- `database update` → DB에 실제 반영
- `Database.Migrate()` → 실행 시 마이그레이션 자동 적용 (조건부 사용 권장)

---

## 🛠️ 실습 절차

### 1. 마이그레이션 패키지 설치

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite

dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### 2. 마이그레이션 생성

```bash
dotnet ef migrations add Init
```

- `/Migrations` 폴더가 생성되고, 초기 테이블 생성 코드가 포함됨
- `AppDbContext`에 정의된 모든 `DbSet<T>` 기준으로 마이그레이션 코드 생성됨

### 3. DB 적용

```bash
dotnet ef database update
```

- 실행 디렉토리에 `records.db`가 생성되고, 테이블이 실제 반영됨

### 4. 앱 코드에서 자동 마이그레이션 적용

`Program.cs`에서 앱 실행 시 마이그레이션을 자동 적용하도록 추가:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // 마이그레이션 자동 적용
}
```

> ⚠ 운영 환경에서는 무조건 자동 적용보다는 조건 분기를 통해 개발/운영 환경을 나눠서 적용하는 것이 안전합니다.

---

## 🧪 자주 겪는 문제와 원인

| 증상                             | 원인 및 해결                                           |
| -------------------------------- | ------------------------------------------------------ |
| `no such table: Records`         | 마이그레이션을 생성하지 않았거나, `update`를 하지 않음 |
| `Migrations 폴더 없음`           | `dotnet ef migrations add`를 하지 않음                 |
| `Migrate()` 호출해도 테이블 없음 | 마이그레이션 코드 자체가 없는 상태 (적용할 게 없음)    |

---

## ✅ 요약 정리

| 작업              | 명령어 또는 방법                                      |
| ----------------- | ----------------------------------------------------- |
| 마이그레이션 생성 | `dotnet ef migrations add Init`                       |
| DB 반영           | `dotnet ef database update` 또는 `Database.Migrate()` |
| 자동 반영         | 앱 실행 시 `Migrate()` 호출                           |
| 운영 대응         | `env.IsDevelopment()` 조건 분기 추천                  |
