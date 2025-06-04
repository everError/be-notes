# Reverse Proxy vs API Gateway

## 개요

Reverse Proxy와 API Gateway는 모두 클라이언트 요청을 적절한 백엔드 서버로 전달하는 역할을 하지만, 사용 목적과 기능이 다릅니다. 이 문서는 두 개념의 차이점을 비교하고, 어떤 상황에서 어떤 기술을 사용해야 하는지 정리합니다.

---

## Reverse Proxy

### ✅ 정의

Reverse Proxy는 **동일한 서비스를 제공하는 여러 개의 서버에 클라이언트 요청을 분산(로드 밸런싱)하는 프록시 서버**입니다.

### ✅ 주요 기능

- **로드 밸런싱**: 클라이언트 요청을 여러 서버로 분배하여 부하를 줄임.
- **SSL 처리**: HTTPS 요청을 처리하고, 내부 서비스로 HTTP 요청 전달 가능.
- **캐싱**: 정적 콘텐츠를 캐싱하여 응답 속도 향상.
- **보안 강화**: 내부 서버를 직접 노출하지 않고, 요청을 필터링 가능.

### ✅ 사용 사례

- **동일한 서비스를 제공하는 여러 개의 서버를 운영할 때**
- **클라이언트의 요청을 부하가 적은 서버로 분산하고 싶을 때**
- **트래픽이 많은 서비스에서 성능 최적화가 필요할 때**

### 📌 예제

#### 🔹 **Reverse Proxy 없이 직접 요청하는 경우**

```
클라이언트 → UserService(5001)
클라이언트 → UserService(5002)
클라이언트 → UserService(5003)
```

➡️ 클라이언트가 서버 주소를 직접 알아야 하며, 부하 분산이 어려움.

#### 🔹 **Reverse Proxy를 사용하여 부하 분산하는 경우**

```
클라이언트 요청 → Reverse Proxy → UserService(5001, 5002, 5003 중 하나로 분산)
```

➡️ 클라이언트는 하나의 엔드포인트(Reverse Proxy)만 사용하며, 내부적으로 부하 분산이 가능.

#### 🔹 **YARP를 사용한 Reverse Proxy 예제** (`Program.cs`)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromMemory(new[]
    {
        new RouteConfig
        {
            RouteId = "user-service",
            ClusterId = "user-cluster",
            Match = new RouteMatch { Path = "/users/{**catch-all}" }
        }
    },
    new[]
    {
        new ClusterConfig
        {
            ClusterId = "user-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                { "destination1", new DestinationConfig { Address = "http://userservice:5001" } },
                { "destination2", new DestinationConfig { Address = "http://userservice:5002" } }
            }
        }
    });

var app = builder.Build();
app.MapReverseProxy();
app.Run();
```

➡️ 클라이언트가 `/users/...` 요청을 보내면 Reverse Proxy가 `UserService(5001, 5002)`로 요청을 분산.

---

## API Gateway

### ✅ 정의

API Gateway는 **여러 종류의 다른 서비스를 운영할 때, 클라이언트 요청을 적절한 서비스로 전달하는 역할을 하는 중앙 관리 포인트**입니다.

### ✅ 주요 기능

- **라우팅**: 클라이언트 요청을 올바른 서비스로 전달.
- **보안 및 인증**: JWT, OAuth 2.0 등을 활용한 인증/인가.
- **Rate Limiting (속도 제한)**: 특정 클라이언트의 과도한 요청 방지.
- **요청 변환**: 클라이언트 요청을 내부 서비스가 이해할 수 있도록 변환.
- **로깅 및 모니터링**: 요청 추적 및 서비스 상태 모니터링.

### ✅ 사용 사례

- **여러 개의 마이크로서비스(API)를 운영할 때**
- **클라이언트가 단일 엔드포인트(API Gateway)만 사용하도록 만들고 싶을 때**
- **JWT 인증, 속도 제한, 요청 변환 등의 기능이 필요할 때**

### 📌 예제

#### 🔹 **API Gateway 없이 클라이언트가 직접 요청하는 경우**

```
클라이언트 → UserService(5001)
클라이언트 → OrderService(5002)
```

➡️ 클라이언트가 각 서비스의 주소를 알아야 하며, 보안 및 인증이 어려움.

#### 🔹 **API Gateway를 사용하여 중앙에서 요청을 관리하는 경우**

```
클라이언트 요청 → API Gateway → 적절한 마이크로서비스(UserService, OrderService 등)로 전달
```

➡️ 클라이언트는 API Gateway 하나만 호출하면 되고, 내부적으로 라우팅이 수행됨.

#### 🔹 **Ocelot을 사용한 API Gateway 예제** (`ocelot.json`)

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/users/{everything}",
      "DownstreamPathTemplate": "/api/user/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "userservice", "Port": 5001 }],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://myapigateway.com"
  }
}
```

➡️ 클라이언트가 `https://myapigateway.com/users/...` 요청을 보내면 API Gateway가 `UserService(5001번 포트)`로 요청을 전달.
➡️ API Gateway에서 **JWT 인증을 검증한 후** 서비스에 요청을 보냄.

---

## Reverse Proxy vs API Gateway 비교

| 비교 항목       | **Reverse Proxy**                         | **API Gateway**                       |
| --------------- | ----------------------------------------- | ------------------------------------- |
| **주요 역할**   | 동일한 서비스의 여러 서버에 트래픽을 분산 | 다양한 마이크로서비스로 요청을 라우팅 |
| **보안 기능**   | 기본 제공 없음 (미들웨어 필요)            | JWT, OAuth 2.0 인증 지원              |
| **로드 밸런싱** | 지원 (Round Robin 등)                     | 일부 지원 (특정 API 로드 밸런싱 가능) |
| **요청 변환**   | 일반적으로 없음                           | 데이터 변환, 헤더 수정 가능           |
| **속도 제한**   | 기본적으로 없음                           | API 요청 수 제한 가능 (Rate Limiting) |
| **사용 목적**   | 성능 최적화 및 트래픽 관리                | API 보안 및 마이크로서비스 관리       |

---

## 결론

✔ **Reverse Proxy는 동일한 서비스를 여러 개 운영할 때, 요청을 부하 분산해야 하는 경우에 사용**  
✔ **API Gateway는 다양한 마이크로서비스를 운영할 때, 요청을 적절한 서비스로 라우팅하고 인증을 적용해야 하는 경우에 사용**  
✔ **Reverse Proxy(YARP)와 API Gateway(Ocelot)를 함께 사용하여 API Gateway + 로드 밸런싱을 최적화할 수도 있음**
