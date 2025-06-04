# 📘 Serilog + Seq 기반 API 요청 로깅 구현 가이드

---

## ✅ 개요

이 문서는 ASP.NET Core 기반 API Gateway 프로젝트(Ocelot 사용)에서 **Serilog + Seq**를 활용하여 HTTP 요청/응답을 로깅하는 방법을 정리한 것입니다.

- 로그는 구조화된 JSON 형태로 출력되며
- 실시간으로 [Seq 대시보드](http://localhost:5341)에서 확인 가능합니다.

---

## 🔧 1. 필수 패키지 설치

```bash
dotnet add package Serilog.AspNetCore

dotnet add package Serilog.Sinks.Seq

dotnet add package Serilog.Enrichers.Environment

dotnet add package Serilog.Enrichers.Process
```

---

## 🧩 2. Serilog 설정 (`Program.cs`)

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
```

---

## 🚏 3. 요청/응답 미들웨어 구현

```csharp
app.Use(async (context, next) =>
{
    var request = context.Request;

    Log.Information("➡️ {Method} {Path} | Query: {Query} | IP: {IP}",
        request.Method,
        request.Path,
        request.QueryString.ToString(),
        context.Connection.RemoteIpAddress?.ToString());

    await next();

    Log.Information("⬅️ {StatusCode} {Method} {Path}",
        context.Response.StatusCode,
        request.Method,
        request.Path);
});
```

> 이 미들웨어는 `UseOcelot()` 호출 **바로 위에 위치**해야 합니다.

---

## 🧪 4. 로그 테스트용 API 추가

```csharp
app.MapGet("/logtest", () =>
{
    Log.Information("🔥 Test log to Seq at {Timestamp}", DateTime.UtcNow);
    return Results.Ok("Logged to Seq");
});
```

- `/logtest` 호출 시 Seq에 로그가 생성되는지 확인

---

## 🐳 5. Docker로 Seq 서버 실행

```bash
docker run --name seq -d -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
```

- 실행 후 브라우저에서 접속: [http://localhost:5341](http://localhost:5341)
- 기본 설정 그대로 사용해도 실시간 로그 확인 가능

---

## 🎯 결과 확인 체크리스트

| 항목                                                | 확인 |
| --------------------------------------------------- | ---- |
| `Program.cs`에서 `UseSerilog()` 호출 여부           | ✅   |
| 로그 레벨 설정 및 필터 (`MinimumLevel.Override`)    | ✅   |
| 미들웨어에서 `Log.Information()` 사용 여부          | ✅   |
| Docker 컨테이너에서 Seq 실행 중인지                 | ✅   |
| 브라우저에서 `http://localhost:5341` 접속 가능 여부 | ✅   |

---

## 🧩 확장 아이디어

- Correlation ID 추적 미들웨어 추가
- Response Body, Header 로깅
- 로그 레벨을 설정 파일(appsettings.json)로 분리
- Slack/Email Sink 연동을 통한 알림 기능

---

이 가이드는 Serilog와 Seq를 사용하여 .NET Gateway API에서 요청 로그를 시각적으로 확인하는 실습을 기반으로 작성되었습니다.
