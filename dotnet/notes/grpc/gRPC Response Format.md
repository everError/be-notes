# gRPC 응답 포맷 이해하기

## ✅ 기본 gRPC 응답 구조

gRPC에서는 호출 결과에 따라 응답 포맷이 구분됩니다.

| 상황          | 응답 내용                       | 예시                                              |
| ------------- | ------------------------------- | ------------------------------------------------- |
| ✅ 정상 응답  | 정의한 Proto 메시지             | `MyResponse { string name = 1; }`                 |
| ❗️ 오류 발생 | gRPC Status + Optional Metadata | `{ "code": 3, "message": "Validation.Required" }` |

---

## ✅ 정상 응답 예시

```csharp
var response = await client.GetDataAsync(new GetDataRequest());
Console.WriteLine(response.Data); // 정상 데이터
```

정상 응답은 **Proto로 정의된 Response 메시지**로 전달됩니다.

---

## ✅ gRPC 오류 응답 구조

오류 발생 시, 클라이언트는 **gRPC Status 객체**를 받습니다.

```json
{
  "code": 3,
  "message": "Validation.Required",
  "details": []
}
```

| 필드명    | 의미                                      |
| --------- | ----------------------------------------- |
| `code`    | gRPC StatusCode (예: 3 = InvalidArgument) |
| `message` | 서버에서 RpcException에 설정한 문자열     |
| `details` | Rich Error Details용 (기본은 비어있음)    |

---

## ✅ gRPC 예외 발생 예시

```csharp
try
{
    await grpcClient.SomeMethodAsync(request);
}
catch (RpcException ex)
{
    Console.WriteLine($"StatusCode: {ex.StatusCode}, Detail: {ex.Status.Detail}");
}
```

---

## ✅ gRPC StatusCode 값 예시

| 코드 | 이름            | 의미                |
| ---- | --------------- | ------------------- |
| 0    | OK              | 정상 응답           |
| 3    | InvalidArgument | 잘못된 파라미터     |
| 5    | NotFound        | 대상을 찾을 수 없음 |
| 13   | Internal        | 서버 내부 오류      |
| 16   | Unauthenticated | 인증 실패           |

---

## ✅ Rich Error Details (고급 에러 전송)

gRPC는 [google.rpc.Status](https://cloud.google.com/apis/design/errors) 를 통해
**details**에 구조화된 메시지를 담을 수 있도록 지원합니다.

이를 위해서는 **Google.Protobuf**, **Grpc.Core.Api** 기반으로 Status를 확장해야 합니다.

---

## ✅ Rich Error 예시 (C#)

```csharp
var status = new Status(StatusCode.InvalidArgument, "Validation error");
var trailers = new Metadata
{
    { "error-code", "Validation.Required" },
    { "error-params", JsonSerializer.Serialize(new [] { "UserName" }) }
};

throw new RpcException(status, trailers);
```

클라이언트에서는 Metadata로 수신:

```csharp
catch (RpcException ex)
{
    var errorCode = ex.Trailers.GetValue("error-code");
    var errorParams = ex.Trailers.GetValue("error-params");
}
```

---

## ✅ 결론

- ✅ 정상 응답은 **Proto로 정의된 메시지**로 리턴된다.
- ✅ 오류 발생 시에만 **gRPC Status (RpcException)** 으로 리턴된다.
- ✅ 기본 RpcException은 StatusCode + Message만 포함한다.
- ✅ 구조화된 에러를 보내고 싶다면 **Metadata (Trailers)** 또는 **Rich Error Details** 사용해야 한다.
- ✅ REST와 달리 JSON 응답은 없으며, gRPC 전용 Status를 사용한다.

---

## ✅ 참고 자료

- [gRPC Error Model (Google API Design)](https://cloud.google.com/apis/design/errors)
- [gRPC Status Codes](https://grpc.io/docs/guides/error/)
