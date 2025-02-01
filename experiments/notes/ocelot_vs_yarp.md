# Ocelot vs YARP

## 개요

Ocelot과 YARP(Yet Another Reverse Proxy)는 .NET 환경에서 API Gateway 역할을 수행하는 오픈소스 라이브러리입니다. 두 기술은 각각의 장점과 사용 사례가 있으며, MSA(Microservices Architecture)에서 API 요청을 라우팅하는 데 활용됩니다.

---

## 비교 정리

| 항목                 | Ocelot                                         | YARP                                                             |
| -------------------- | ---------------------------------------------- | ---------------------------------------------------------------- |
| **개발사**           | Microsoft OSS 기반 커뮤니티                    | Microsoft 공식 개발                                              |
| **주요 역할**        | API Gateway, 서비스 간 라우팅, 인증/권한 관리  | 고성능 Reverse Proxy 및 API Gateway                              |
| **구현 방식**        | 미들웨어 기반 (`.NET Core Middleware`)         | Reverse Proxy (`.NET Kestrel 내부 최적화`)                       |
| **라우팅 방식**      | JSON 기반 `ocelot.json` 설정 파일 사용         | C# 코드 기반 설정 가능                                           |
| **성능**             | 상대적으로 무겁고 설정이 많음                  | 경량, 고성능 (Kestrel 최적화)                                    |
| **사용 사례**        | 마이크로서비스 아키텍처에서 API Gateway 역할   | 로드 밸런싱, 서비스 간 트래픽 분산, 내부 서비스 간 Reverse Proxy |
| **로드 밸런싱**      | 기본 지원 (Round Robin 등)                     | 동적 클러스터 관리 가능 (자동 서비스 등록 및 변경)               |
| **보안 기능**        | JWT 인증, OAuth 2.0, OpenID Connect, RBAC 지원 | 기본 제공 없음 (별도 미들웨어 필요)                              |
| **실시간 설정 변경** | 제한적                                         | Hot Reload 지원                                                  |
| **Kubernetes 연동**  | 가능하지만 기본 지원 없음                      | Kubernetes 서비스 자동 검색 가능                                 |
| **WebSocket 지원**   | 기본 지원 안함 (추가 설정 필요)                | 기본 지원                                                        |
| **사용 환경**        | API Gateway 구축 시 적합                       | 내부 서비스 프록시 및 트래픽 관리                                |

---

## Ocelot의 특징

✅ API Gateway 역할 수행 (서비스 디스커버리, 인증, 로깅, 캐싱 지원)  
✅ JSON 기반 설정 (`ocelot.json`)으로 관리 용이  
✅ JWT, OAuth 2.0 등 인증 기능 내장  
✅ Kubernetes 환경에서도 사용 가능하지만 직접적인 통합 지원은 부족  
✅ 성능은 YARP보다 상대적으로 낮지만, 기능이 풍부함

**사용 사례:**

- 복잡한 마이크로서비스 환경에서 API Gateway 필요 시
- 인증 및 보안 기능이 필요한 서비스
- 외부 API 관리 및 정책 적용

---

## YARP의 특징

✅ 고성능 Reverse Proxy (Kestrel 기반)  
✅ 동적 서비스 라우팅 및 로드 밸런싱 지원  
✅ WebSocket 및 HTTP/2 기본 지원  
✅ Kubernetes와의 통합 가능  
✅ C# 코드 기반 설정 (Hot Reload 지원)  
✅ API Gateway보다는 내부 트래픽 프록시 및 라우팅에 적합

**사용 사례:**

- 내부 서비스 간 트래픽 프록시 역할
- Kubernetes 기반 마이크로서비스에서 Reverse Proxy 필요 시
- 실시간 트래픽 조정 및 로드 밸런싱이 필요한 환경

---

## 결론

| 선택 기준                                | 추천 기술 |
| ---------------------------------------- | --------- |
| **API Gateway 구축**                     | ✅ Ocelot |
| **고성능 Reverse Proxy 필요**            | ✅ YARP   |
| **인증 및 보안 기능 포함된 솔루션**      | ✅ Ocelot |
| **Kubernetes 네이티브 환경**             | ✅ YARP   |
| **실시간 트래픽 관리 및 WebSocket 지원** | ✅ YARP   |

YARP는 **고성능이 필요한 Reverse Proxy 및 서비스 간 라우팅**에 적합하며, Ocelot은 **API Gateway 기능이 필요한 MSA 환경**에서 강력한 보안 및 정책 관리 기능을 제공합니다.

**MSA 환경에서 API Gateway 역할이 필요한 경우** → **Ocelot** 추천  
**Reverse Proxy로 내부 서비스 트래픽을 효율적으로 관리하고 싶은 경우** → **YARP** 추천

---

추가로 환경에 따라 두 기술을 **함께 사용하여 API Gateway + Reverse Proxy 역할을 최적화**할 수도 있습니다.
