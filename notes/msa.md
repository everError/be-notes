# MSA (Microservices Architecture)

## 개요

MSA(Microservices Architecture)는 대규모 애플리케이션을 여러 개의 독립적인 서비스로 나누어 관리하는 아키텍처입니다. 각 서비스는 독립적으로 배포, 확장 및 운영될 수 있으며, API를 통해 서로 통신합니다.

---

## 주요 개념

1. **서비스 독립성**

   - 각 서비스는 개별적으로 개발, 배포 및 확장 가능.
   - 특정 서비스 장애가 전체 시스템에 영향을 미치지 않도록 격리.

2. **서비스 간 통신 방식**

   - 서비스 간 통신 방식은 **동기식**과 **비동기식**으로 나뉨.

   **동기식 통신 (Synchronous Communication)**

   - RESTful API (HTTP) 또는 gRPC를 사용하여 요청-응답 방식으로 통신.
   - 서비스 간 강한 결합도가 발생할 수 있으며, 장애 전파 가능성이 있음.
   - 예제: `OrderService`가 `UserService`의 API를 호출하여 사용자 정보를 조회.

   **비동기식 통신 (Asynchronous Communication)**

   - 메시지 브로커(RabbitMQ, Kafka)를 사용하여 서비스 간 이벤트 기반 통신.
   - 서비스 간 결합도를 줄이고, 비동기 데이터 처리가 가능.
   - 예제: `PaymentService`가 결제 완료 이벤트를 메시지 큐에 발행하면, `OrderService`가 이를 구독하여 주문 상태를 업데이트.

3. **데이터 분리**

   - 각 서비스는 자체적인 데이터베이스를 가짐(Polyglot Persistence 가능).
   - 서비스 간 직접적인 DB 공유를 최소화하고 API 또는 이벤트 기반 방식으로 데이터 동기화.

4. **확장성 및 유연성**

   - 개별 서비스 단위로 확장 가능(예: 특정 서비스만 더 많은 인스턴스를 실행).
   - 특정 기술 스택(Node.js, .NET, Java 등)을 개별 서비스마다 다르게 적용 가능.

5. **API Gateway 활용**
   - 클라이언트는 개별 서비스가 아닌 API Gateway를 통해 서비스에 접근.
   - 보안, 인증, 로깅, 로드 밸런싱, 캐싱 등의 기능을 API Gateway에서 수행.
   - MSA에서 API Gateway는 다음과 같은 주요 역할을 수행함:
     - **Reverse Proxy**: 클라이언트 요청을 적절한 서비스로 전달 (YARP, Ocelot 활용 가능)
     - **BFF (Backend for Frontend)**: 특정 클라이언트(Web, Mobile 등)에 맞는 API를 제공
     - **인증 및 권한 관리**: JWT 또는 OAuth 2.0을 통해 사용자 인증을 처리하고, API 접근을 제어
     - **트래픽 관리 및 로드 밸런싱**: 특정 서비스에 과부하가 걸리지 않도록 요청을 분산
     - **요청/응답 변환**: 클라이언트 요청을 내부 서비스가 이해할 수 있는 형태로 변환

---

## Observability (모니터링 및 로깅)

마이크로서비스 환경에서는 각 서비스의 상태를 추적하고, 장애를 감지하는 것이 중요합니다. 이를 위해 Observability(가시성) 관련 기술을 적용해야 합니다.

1. **분산 트레이싱 (Distributed Tracing)**

   - 서비스 간 요청 흐름을 추적하고 성능 병목을 파악.
   - OpenTelemetry, Jaeger, Zipkin 등을 활용하여 서비스 호출 간의 관계를 시각화.

2. **로깅 (Centralized Logging)**

   - 모든 마이크로서비스의 로그를 중앙 집중화하여 분석.
   - ELK Stack (Elasticsearch, Logstash, Kibana), Grafana Loki 등을 활용.

3. **메트릭 수집 및 모니터링**

   - 서비스의 CPU, 메모리 사용량, 요청 처리 속도 등을 모니터링.
   - Prometheus + Grafana를 활용하여 실시간 데이터 시각화 가능.

4. **헬스 체크 (Health Checks) 및 경고 시스템**
   - .NET의 `IHealthCheck` 기능을 활용하여 서비스의 정상 동작 여부 확인.
   - 문제가 발생하면 알림 시스템(Slack, PagerDuty, OpsGenie)과 연동하여 즉시 대응 가능.

---

## 인증 및 권한 관리

MSA에서 인증 및 권한 관리는 보안성을 유지하면서도 개별 서비스가 인증 로직을 직접 처리하지 않도록 설계해야 합니다. 일반적으로 **API Gateway에서 인증을 처리하고** 이후 요청을 내부 서비스로 전달하는 방식을 사용합니다.

### 1. **JWT (JSON Web Token) 기반 인증**

- 클라이언트가 로그인하면 **JWT Access Token**을 발급받고 이후 모든 요청에 포함.
- API Gateway가 JWT를 검증한 후, 유효한 요청만 내부 서비스로 전달.
- 토큰 만료 시 Refresh Token을 이용하여 새 Access Token 발급 가능.

### 2. **OAuth 2.0 / OpenID Connect**

- 인증을 외부 인증 서버(IdentityServer, Keycloak 등)에 위임.
- API Gateway가 OAuth 2.0을 이용해 인증 후, 내부 서비스에 필요한 사용자 정보 전달.
- 서비스 간 접근 권한을 조정하기 위해 **OAuth Scopes**를 사용.

### 3. **서비스 간 인증 (Service-to-Service Authentication)**

- 서비스 간 직접 호출이 필요한 경우 **mTLS, API Key, OAuth Client Credentials Flow**를 사용.
- Zero Trust 보안 모델을 적용하여 각 서비스의 접근 권한을 엄격히 관리.

### 4. **RBAC (Role-Based Access Control) 및 ABAC (Attribute-Based Access Control)**

- 특정 API 또는 기능을 호출할 수 있는 권한을 사용자 역할(Role) 또는 속성(Attribute) 기반으로 설정.
- API Gateway가 RBAC 또는 ABAC 정책을 적용하여 요청을 필터링할 수 있음.

### 5. **Federated Authentication (연합 인증)**

- 기업 환경에서는 **SAML, OAuth 2.0, OpenID Connect**를 사용하여 중앙 인증 서버를 통해 여러 마이크로서비스에서 동일한 인증을 공유.

---

## MSA의 장점

✅ **유지보수성**: 서비스가 작고 독립적이기 때문에 코드 관리가 쉬움.
✅ **확장성**: 특정 서비스만 확장 가능하여 리소스 최적화.
✅ **배포 유연성**: 개별 서비스만 업데이트 가능(Blue-Green 배포, Canary 배포 등).
✅ **장애 격리**: 한 서비스 장애가 전체 시스템에 영향을 주지 않음.

---

## MSA의 단점 및 해결 방법

❌ **서비스 간 통신 복잡성** → API Gateway, 메시지 브로커(RabbitMQ, Kafka) 활용.
❌ **데이터 일관성 문제** → 이벤트 소싱(Event Sourcing) 및 CQRS 패턴 적용.
❌ **배포 및 운영 어려움** → CI/CD 자동화, Kubernetes 등 컨테이너 오케스트레이션 사용.

---

## 결론

MSA는 대규모 애플리케이션을 보다 유연하고 확장성 있게 관리하기 위한 아키텍처입니다. .NET 기반의 MSA에서는 **.NET Aspire**, **YARP**, **Ocelot** 등의 기술을 활용하여 API Gateway를 구성하고, 마이크로서비스 간 통신을 최적화할 수 있습니다.
