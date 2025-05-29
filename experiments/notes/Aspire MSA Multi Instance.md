# .NET Aspire로 다중 인스턴스 MSA 실행하기

이 문서는 .NET Aspire 환경에서 MSA(Microservice Architecture) 구성 중, **동일 서비스의 여러 인스턴스를 다른 포트로 실행하는 방법**과 **자주 발생하는 문제 및 해결 방법**을 정리한 가이드입니다.

---

## ✅ 사용 기술 및 구성

- .NET 8 + .NET Aspire SDK
- `DistributedApplication.CreateBuilder` 사용
- 다중 `AddProject<>()` 호출로 MSA 구성

---

## 🧱 예시 코드 구조

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// auth-service의 인스턴스를 5001, 5002 포트에서 실행
builder.AddProject<Projects.auth_service>("auth-service")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(name: "auth-service-http-1", port: 5001)
    .WithHttpEndpoint(name: "auth-service-http-2", port: 5002);

// API Gateway 실행 (5000 포트에서 실행) - 해당 프로젝트의 launchSettings
builder.AddProject<Projects.gateway_service>("gateway-service");

builder.AddProject<Projects.data_service>("data-service-1")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(name: "data-service-http-1", port: 5003)
    .WithHttpEndpoint(name: "data-service-http-2", port: 5004);

builder.Build().Run();
```

---

## ⚙️ 핵심 포인트

### 1. `WithHttpEndpoint()`는 포트뿐만 아니라 이름도 유일해야 함

- 각 서비스마다 endpoint 이름이 중복되면 Aspire 내부에서 오류 발생
- `Endpoint with name 'http' already exists` 오류 발생 시 이름 변경 필요

**해결 예시:**

```csharp
.WithHttpEndpoint(name: "data-service-http-1", port: 5003)
.WithHttpEndpoint(name: "data-service-http-2", port: 5004)
```

---

### 2. `launchSettings.json`과 포트 충돌 주의

- `launchSettings.json`에서 `applicationUrl`을 지정해두면 Aspire가 지정한 포트와 충돌할 수 있음
- 이로 인해 실제 실행 시 **중복 포트 오류** 발생

**해결 방법:**

- `applicationUrl` 제거 또는 주석 처리
- 또는 Aspire 설정에서 `.WithEnvironment("ASPNETCORE_ENVIRONMENT", ...)`만 명시

---

## 🧪 자주 겪는 문제와 해결책

| 증상                                | 원인 및 해결                                                       |
| ----------------------------------- | ------------------------------------------------------------------ |
| 포트 바인딩 실패                    | `launchSettings.json`과 충돌 또는 endpoint 이름 중복               |
| `Endpoint with name already exists` | 동일한 endpoint 이름을 여러 인스턴스에 사용함 → 고유 이름으로 변경 |

---

## ✅ 요약

| 주제                | 요약                                                          |
| ------------------- | ------------------------------------------------------------- |
| 다중 인스턴스 실행  | `.AddProject(...).WithHttpEndpoint(...)`를 포트별로 반복 등록 |
| endpoint 충돌       | `name`을 고유하게 설정                                        |
| launchSettings 영향 | 되도록 제거하거나 `ASPNETCORE_URLS`와 일치시키기              |

---

이 구조를 통해 Aspire 기반 MSA 프로젝트에서도 실제 운영 환경과 유사한 다중 인스턴스 테스트 구성이 가능합니다.
