# 📘 Serilog & Seq 개념 정리

---

## 🔹 Serilog란?

### ✅ 개요

Serilog는 .NET 플랫폼을 위한 **구조화 로깅(Structured Logging)** 프레임워크이다. 전통적인 텍스트 기반 로깅이 아닌, 로그를 \*\*구조화된 데이터(JSON 등)\*\*로 저장하여 필터링, 분석, 시각화가 쉽도록 설계되었다.

### ✅ 주요 특징

- **구조화된 로그 메시지**: key-value 형태로 로그 데이터를 저장
- **다양한 출력(Sink)** 지원: 파일, 콘솔, Seq, Elasticsearch, Redis 등
- **Enrichment 지원**: 로그 메시지에 context 정보(Correlation ID 등)를 추가
- **미들웨어 및 DI 친화적 구성**: ASP.NET Core와 쉽게 통합 가능
- **Sink 단위 분리 설정 가능**: 로그 레벨 별로 서로 다른 저장소로 분기 가능

### ✅ 예시

```csharp
Log.Information("User {UserId} requested order {OrderId}", userId, orderId);
```

```json
{
  "@t": "2025-05-14T02:30:00.123Z",
  "@m": "User 42 requested order 12345",
  "UserId": 42,
  "OrderId": 12345
}
```

---

## 🔹 Seq란?

### ✅ 개요

Seq는 구조화 로그를 수집, 저장, 분석할 수 있는 **웹 기반 로그 뷰어 및 분석 도구**이다. Serilog와 긴밀하게 통합되며, 실시간 필터링, 검색, 시각화 기능을 제공한다.

### ✅ 주요 기능

- **구조화 로그 저장소** 역할 수행
- **SQL-like 쿼리 문법 지원**으로 강력한 필터링 가능
- **웹 UI 기반 대시보드 제공**
- **경고 및 알림 트리거 설정 가능**
- **Docker 및 self-hosting 설치 가능**

### ✅ 대시보드 특징

- 실시간 로그 스트림
- 다양한 메타데이터(`@level`, 사용자 정의 필드 등) 기반 필터링
- Correlation ID, Request ID 기반 추적 가능

---

## 🔹 Serilog + Seq 조합의 장점

| 기능             | 설명                                                |
| ---------------- | --------------------------------------------------- |
| 구조화된 로그    | 쿼리 및 분석이 가능한 key-value 로그 구조           |
| 유연한 Sink 구성 | 콘솔, 파일, DB, Seq 등 다양한 대상에 로그 분기 가능 |
| 실시간 검색      | 브라우저 UI를 통해 즉시 로그 분석 가능              |
| MSA와의 적합성   | 요청 간 Correlation ID 추적에 용이                  |
| Docker 기반 설치 | Seq를 Docker로 손쉽게 배포 가능                     |

---

## 🔹 Serilog 구성 요소

### 1. LoggerConfiguration

Serilog 설정의 시작점으로, Sink, Enricher, Filter 등을 체이닝 방식으로 구성한다.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.FromLogContext()
    .CreateLogger();
```

### 2. Sink

로그 출력 대상 구성 요소. 대표 Sink로는 Console, File, Seq, Elasticsearch, SQLite 등이 있다.

### 3. Enricher

로그에 메타데이터를 추가하는 구성 요소. 예: 사용자 정보, 서비스 이름, 환경 정보 등.

```csharp
.Enrich.WithProperty("App", "GatewayService")
.Enrich.FromLogContext()
```

### 4. ASP.NET Core 연동

ASP.NET Core 미들웨어로 `UseSerilogRequestLogging()`을 사용할 수 있으며, 필요 시 custom 미들웨어 구현도 가능하다.

---

## 🔹 Serilog + Seq 실습 예시

### Program.cs 구성

```csharp
builder.Host.UseSerilog((context, config) => {
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Seq("http://localhost:5341");
});
```

### 요청 및 응답 로깅 미들웨어 예시

```csharp
app.Use(async (context, next) => {
    Log.Information("➡️ Request {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
    Log.Information("⬅️ Response {StatusCode}", context.Response.StatusCode);
});
```

---

## 🔹 대시보드 비교: Seq vs Kibana

| 항목          | Seq                | Kibana                                |
| ------------- | ------------------ | ------------------------------------- |
| 설치 난이도   | 쉬움 (Docker 지원) | 비교적 복잡 (Elasticsearch 연동 필요) |
| 사용 목적     | 구조화 로그 뷰어   | 전체 로그/데이터 분석 도구            |
| UI 인터페이스 | 직관적, .NET 중심  | 강력하지만 복잡함                     |
| 사용 사례     | .NET 기반 시스템   | 멀티소스 통합 시스템                  |

---

## 🔹 확장 방향

- `Serilog.Sinks.Elasticsearch` 사용 시 Kibana 연동 가능
- `OpenTelemetry` 연동을 통한 trace-log 통합 시각화 가능
- `Correlation ID`, `Trace ID` 자동 전파 미들웨어 구성
- Slack, Email, Teams 등 알림 연동 Sink 구성 가능
