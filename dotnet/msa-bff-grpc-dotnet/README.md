# BFF + gRPC (.NET MSA Example)

이 프로젝트는 .NET 기반 마이크로서비스 아키텍처(MSA)에서 BFF(Backend for Frontend)와 gRPC를 활용한 통신 구조를 실습하기 위한 예제입니다. BFF는 클라이언트 요청을 수신하고 여러 gRPC 서비스를 호출하여 응답을 조합합니다.

## 주요 특징

- BFF가 클라이언트에 최적화된 단일 응답 구성
- 내부 서비스 간 통신은 gRPC 사용
- 인증, 사용자, 상품 등의 도메인을 gRPC 서비스로 분리
- JWT 기반 인증 및 Redis 연동 포함

## 사용 기술 및 라이브러리

- **.NET 8**, **C# 12**
- **gRPC**, **Grpc.AspNetCore**: 서비스 간 통신
- **JWT (System.IdentityModel.Tokens.Jwt)**: 인증 및 인가
- **Redis (StackExchange.Redis)**: 토큰 저장 및 Pub/Sub 처리
- **BFF 서버 (ASP.NET Core)**: REST API 노출 및 응답 가공
- **Docker Compose**: 다중 서비스 실행 환경 구성
