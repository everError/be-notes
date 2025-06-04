# DotNet Notes

## 소개

이 저장소는 .NET, EF Core 등 닷넷 생태계의 백엔드 기술, 실험, 실습, 그리고 주요 아키텍처/문제 해결 경험을 체계적으로 정리하기 위한 공간입니다.

1. [.NET 8](./dotnet8/)

---

## 디렉토리 구조

```

dotnet/
├── dotnet8/                           # 실험/실습 프로젝트 및 클라이언트 샘플
│   ├── clients/                       # API 호출 등 테스트용 클라이언트
│   ├── services/                      # 실습용 서비스 프로젝트
│   ├── .gitignore
│   └── README.md                      # experiments 폴더 안내
├── notes/                             # 주요 기술/이슈 정리
│   ├── concurrency-control/           # 동시성 제어 관련 정리
│   ├── EF Core/                       # EF Core 관련 정리
│   ├── issues/                        # 트러블슈팅/이슈 정리
│   ├── Test/                          # 테스트 관련 정리
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
