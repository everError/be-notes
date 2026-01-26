# .NET 10 Code-First gRPC Example

.NET 10 환경에서 **protobuf-net 기반 Code-First gRPC**를 사용하여  
`BFF → gRPC Service` 구조를 구성하는 간단한 예제입니다.

proto 파일을 직접 작성하지 않고 **C# 인터페이스와 DTO**를 계약(Contract)으로 사용합니다.

---

## 구성

```

src/
├─ Contracts/        # gRPC 계약 (공유)
├─ UserService/     # gRPC Server
└─ Bff/             # gRPC Client (BFF)
-Proto/             # gRPC proto export

```

- **Contracts**만 공유
- Domain / EF / Infra 공유 ❌

---

## 사용 기술

- .NET 10
- ASP.NET Core gRPC
- protobuf-net.Grpc (Code-First)
- HTTP/2 + Protobuf

---

## 패키지

### Contracts

```bash
dotnet add package protobuf-net.Grpc
```

### gRPC Service

```bash
dotnet add package protobuf-net.Grpc.AspNetCore
```

### BFF (Client)

```bash
dotnet add package protobuf-net.Grpc.Client
```

---

## 핵심 코드 예시

### gRPC 계약 (Code-First)

```csharp
[ServiceContract]
public interface IUserGrpcService
{
    ValueTask<UserDto> GetUserAsync(GetUserRequest request);
}
```

```csharp
[ProtoContract]
public class UserDto
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = default!;
}
```

---

### gRPC Service 등록

```csharp
builder.Services.AddCodeFirstGrpc();

app.MapGrpcService<UserGrpcService>();
```

---

### BFF gRPC Client 등록

```csharp
builder.Services.AddCodeFirstGrpcClient<IUserGrpcService>(o =>
{
    o.Address = new Uri("https://localhost:5001");
});
```

---

## 오류 처리 규칙

- 서버에서는 반드시 `RpcException`으로 변환하여 throw
- `StatusCode` + `Status.Message`만 사용
- 언어 중립 (타언어 gRPC Client 대응 가능)

```csharp
throw new RpcException(
    new Status(StatusCode.NotFound, "USER_NOT_FOUND")
);
```

---

## Code-First 사용 이유

- proto 작성/관리 비용 감소
- .NET 개발 생산성 향상
- 내부 마이크로서비스 통신에 최적
- 필요 시 **proto export**로 타언어 client 대응 가능

---

## 주의 사항

- Code-First는 **C# 계약이 Single Source of Truth**
- export된 proto는 수정하지 않음
- 외부 공개 API로 확장 시 proto-first 전환 고려

---

## 요약

> **이 예제는 .NET 중심 조직에서
> 빠른 gRPC 도입과 운영을 위한 실무용 Code-First 패턴입니다.**
