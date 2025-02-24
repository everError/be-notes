# DotNet Notes

## 소개

이 레포지토리는 .NET과 EF Core 등을 활용하여 API 백엔드 서버를 개발하며 학습한 내용을 기록하기 위해 만들어졌습니다. 기술에 대한 설명과 실습 프로젝트를 체계적으로 정리하여 학습의 깊이를 더하고, 필요 시 참조할 수 있는 지침서로 활용하고자 합니다.

## 목적

1. **학습 기록**: .NET, EF Core, API 개발, MSA(Microservices Architecture) 등 다양한 기술에 대한 학습 내용을 정리합니다.
2. **실습 프로젝트**: 이론적으로 배운 기술을 실제로 구현해보고 익히는 공간을 제공합니다.

### 개발

- [.Net8](./experiments/)

## 디렉토리 구조

```
.
├── notes/                                      # 기술 설명 및 학습 내용 정리
│   ├── jwt.md                                  # JWT(Json Web Token)에 대한 설명
│   ├── dotnet_aspire.md                        # .Net Aspire에 대한 설명
│   ├── msa.md                                  # MSA(Microservices Architecture)에 대한 설명
│   ├── websocket.md                            # WebSocket에 대한 설명
│   ├── redis.md                                # redis 정리
├── experiments/
│   ├── README.md                               # 실습 프로젝트 소개
│   ├── services/                               # 실습 프로젝트
│   ├── notes/                                  # 실험적인 학습 내용 정리
│   │   ├── ocelot_vs_yarp.md                   # Ocelot과 YARP 비교 정리
│   │   ├── reverse_proxy_vs_api_gateway.md     # ReverseProxy와 ApiGateway 비교 정리
│   │   ├── websocket_reference.md              # WebSocket 사용
└── README.md
```
