# ✅ Ocelot API Gateway 타임아웃 문제와 해결 방법

## 1️⃣ 겪었던 문제

- Ocelot을 사용하여 Backend 서비스(API)를 프록시할 때
- 클라이언트(Axios, Swagger 등)가 장시간 처리되는 요청을 보내면
- 약 **90초 후 요청이 강제로 종료**되고, **HTTP 499 (Client Closed Request)** 오류가 발생함
- Kestrel 및 Axios에 Timeout 설정을 적용해도 동일한 현상 발생

## 2️⃣ 원인 분석

- Ocelot의 기본 동작에서는 **QoSOptions가 없으면 내부적으로 90초의 기본 Timeout이 적용됨**
- `GlobalConfiguration.Timeout` 또는 `Route.Timeout` 설정만으로는 해결되지 않음
- **Ocelot의 Timeout은 Polly 기반의 QoS Middleware를 통해서만 제대로 적용 가능**

## 3️⃣ 최종 해결 방법

- `Ocelot.Provider.Polly` NuGet 패키지를 설치

  ```bash
  dotnet add package Ocelot.Provider.Polly
  ```

- Ocelot 서비스 등록 시 Polly Provider 추가

  ```csharp
  builder.Services.AddOcelot().AddPolly();
  ```

- 각 Route에 QoSOptions를 명시하여 Timeout 설정 적용

  ```json
  {
    "Routes": [
      {
        "DownstreamPathTemplate": "/api/{everything}",
        "ServiceName": "auth",
        "UpstreamPathTemplate": "/api/auth/{everything}",
        "UpstreamHttpMethod": ["Get", "Post", "Put", "Delete"],
        "SwaggerKey": "auth",
        "QoSOptions": {
          "ExceptionsAllowedBeforeBreaking": 999999,
          "DurationOfBreak": 0,
          "TimeoutValue": 600000 // 10분
        }
      }
    ]
  }
  ```

## 4️⃣ 결과

- Ocelot을 통한 요청의 타임아웃이 **설정한 10분까지 정상적으로 유지됨**
- Axios, Swagger에서도 더 이상 90초 후 연결이 끊기는 현상이 발생하지 않음

---

## ✅ 요약

| 항목          | 설정                                 |
| ------------- | ------------------------------------ |
| 사용한 패키지 | Ocelot.Provider.Polly                |
| 서비스 등록   | `AddOcelot().AddPolly()`             |
| 적용한 설정   | `QoSOptions.TimeoutValue = 600000`   |
| 효과          | 요청 타임아웃이 10분으로 정상 처리됨 |
