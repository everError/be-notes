# DotNet Notes

## 소개

이 저장소는 .NET, EF Core 등 닷넷 생태계의 백엔드 기술, 실험, 실습, 그리고 주요 아키텍처/문제 해결 경험을 체계적으로 정리하기 위한 공간입니다.

### 주요 프로젝트

1. [MSA 기반 .NET 8 웹 API 예제](./msa-webapi-dotnet/)
   - Ocelot 기반 API Gateway를 통해 클라이언트 요청을 내부 API 서비스로 라우팅
   - JWT 기반 인증 마이크로서비스 (`auth-service`)
   - 도메인별 HTTP API 서비스
2. [MSA 기반 BFF + gRPC 예제](./msa-bff-grpc-dotnet/)
   - gRPC 기반 내부 마이크로서비스 간 통신
   - BFF(Backend for Frontend) 서버가 클라이언트 맞춤형 응답 조합 및 가공 처리 담당
   - REST 대신 gRPC + BFF 구조로 경량화 및 응답 최적화 지향

---

### Notes

- [트랜잭션 및 SaveChanges 재시도를 위한 Attribute 기반 구조 설계](./notes/concurrency-control/Transaction%20SaveRetry%20Attribute.md)
- [개발 이슈](./notes/issues/)
- [EFCore](./notes/EF%20Core/)

---

## 디렉토리 구조

```

dotnet/
├── msa-webapi-dotnet/                             # 실험/실습 프로젝트 및 클라이언트 샘플
│   ├── clients/                                   # API 호출 등 테스트용 클라이언트
│   ├── services/                                  # 실습용 서비스 프로젝트
│   ├── .gitignore
│   └── README.md                                  # experiments 폴더 안내
├── msa-webapi-dotnet/                             # 실험/실습 프로젝트 및 클라이언트 샘플
├── notes/                                         # 주요 기술/이슈 정리
│   ├── concurrency-control/                       # 동시성 제어 관련 정리
│   ├── EF Core/                                   # EF Core 관련 정리
│   ├── issues/                                    # 트러블슈팅/이슈 정리
│   ├── Test/                                      # 테스트 관련 정리
│   ├── Aspire MSA Multi Instance.md
│   ├── channel.md
│   ├── dotnet\_aspire.md
│   ├── Efcore Sqlite Migration.md
│   ├── ocelot\_vs\_yarp.md
│   ├── reverse\_proxy\_vs\_api\_gateway.md
│   ├── serilog\_seq Concept.md
│   ├── serilog\_seq.md
│   └── websocket\_reference.md
    ...

```
