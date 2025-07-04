# 🧭 gRPC + Swagger 설정 가이드

## 📦 NuGet 패키지 설치

```bash
dotnet add package Microsoft.AspNetCore.Grpc.Swagger
```

> 🔹 `Grpc.AspNetCore.GrpcJsonTranscoding`은 `Microsoft.AspNetCore.Grpc.Swagger`에 포함되어 따로 설치할 필요 없음

---

## 📄 .proto 설정

```proto
// Greeter 서비스는 이름을 받아 인사 응답을 반환합니다.
service Greeter {
  // SayHello는 HelloRequest를 받아 HelloReply를 반환합니다.
  rpc SayHello (HelloRequest) returns (HelloReply) {
    option (google.api.http) = {
      get: "/v1/hello"
    };
  }
}

// 클라이언트로부터 이름을 받기 위한 메시지
message HelloRequest {
  string name = 1; // 인사할 이름
}

// 서버가 반환하는 인사 메시지
message HelloReply {
  string message = 1; // 인사 응답
}
```

`.csproj` 파일 내에 다음 항목 추가:

```xml
<PropertyGroup>
  <IncludeHttpRuleProtos>true</IncludeHttpRuleProtos>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>

<ItemGroup>
  <Protobuf Include="Protos\greet.proto" GrpcServices="Server" />
</ItemGroup>
```

---

## ⚙️ Program.cs 구성

```csharp
builder.Services
    .AddGrpc()
    .AddJsonTranscoding();

builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "gRPC Transcoding", Version = "v1" });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "MyGrpcProject.xml");
    c.IncludeXmlComments(xmlPath);
    c.IncludeGrpcXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGrpcService<GreeterService>();
app.Run();
```

---

## ✍️ C# 주석 작성법 예시

```csharp
/// <summary>
/// 이름을 받아 인사 메시지를 반환합니다.
/// </summary>
/// <param name="request">요청 메시지</param>
/// <param name="context">gRPC 서버 컨텍스트</param>
/// <returns>인사 메시지를 포함한 응답</returns>
public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
{
    return Task.FromResult(new HelloReply
    {
        Message = $"Hello, {request.Name}"
    });
}
```

> 📌 이 주석은 XML 문서화 설정과 함께 Swagger UI에 표시됩니다.

---

## ✅ 결과

- `/swagger` 경로에서 Swagger UI로 gRPC 메서드 확인 및 테스트 가능
- HTTP GET/POST 방식으로 gRPC 호출 가능 (JSON Transcoding)
- C#의 `///` 주석과 `.proto` 주석을 기반으로 문서 자동 생성 가능

---

## 📒 참고 문서

- [gRPC JSON Transcoding + Swagger (MS Docs)](https://learn.microsoft.com/aspnet/core/grpc/json-transcoding-openapi)
- [Microsoft.AspNetCore.Grpc.Swagger on NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Grpc.Swagger)
