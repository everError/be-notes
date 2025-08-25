### \#\# 1. 💬 단항 RPC (Unary RPC)

가장 일반적인 요청-응답 방식입니다. 클라이언트가 요청을 한 번 보내면 서버가 응답을 한 번 보냅니다.

- **`.proto` 정의:**
  ```protobuf
  rpc GetItem(GetItemRequest) returns (ItemResponse);
  ```
- **실행되는 핸들러:** `UnaryServerHandler`
- **C\# 시그니처:**
  ```csharp
  public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
      TRequest request,
      ServerCallContext context,
      UnaryServerHandler<TRequest, TResponse> continuation)
  ```

---

### \#\# 📤 클라이언트 스트리밍 RPC (Client Streaming RPC)

클라이언트가 여러 개의 메시지를 순차적으로(스트림으로) 보내면, 서버는 모든 메시지를 다 받은 후에 응답을 한 번 보냅니다. 대용량 파일 업로드와 같은 시나리오에 사용됩니다.

- **`.proto` 정의:**
  ```protobuf
  rpc UploadFile(stream UploadRequest) returns (UploadResponse);
  ```
- **실행되는 핸들러:** `ClientStreamingServerHandler`
- **C\# 시그니처:**
  ```csharp
  public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
      IAsyncStreamReader<TRequest> requestStream,
      ServerCallContext context,
      ClientStreamingServerHandler<TRequest, TResponse> continuation)
  ```

---

### \#\# 📥 서버 스트리밍 RPC (Server Streaming RPC)

클라이언트가 요청을 한 번 보내면, 서버가 여러 개의 메시지를 순차적으로(스트림으로) 보냅니다. 대용량 데이터 조회나 실시간 데이터 피드 구독 등에 사용됩니다.

- **`.proto` 정의:**
  ```protobuf
  rpc SubscribeToFeed(FeedRequest) returns (stream FeedUpdate);
  ```
- **실행되는 핸들러:** `ServerStreamingServerHandler`
- **C\# 시그니처:**
  ```csharp
  public override Task ServerStreamingServerHandler<TRequest, TResponse>(
      TRequest request,
      IServerStreamWriter<TResponse> responseStream,
      ServerCallContext context,
      ServerStreamingServerHandler<TRequest, TResponse> continuation)
  ```

---

### \#\# ⇄ 양방향 스트리밍 RPC (Bidirectional Streaming RPC)

클라이언트와 서버가 서로 독립적으로 여러 개의 메시지를 주고받습니다. 실시간 채팅이나 온라인 게임과 같은 시나리오에 사용됩니다.

- **`.proto` 정의:**
  ```protobuf
  rpc Chat(stream ChatMessage) returns (stream ChatMessage);
  ```
- **실행되는 핸들러:** `DuplexStreamingServerHandler`
- **C\# 시그니처:**
  ```csharp
  public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
      IAsyncStreamReader<TRequest> requestStream,
      IServerStreamWriter<TResponse> responseStream,
      ServerCallContext context,
      DuplexStreamingServerHandler<TRequest, TResponse> continuation)
  ```

---

### \#\# 📊 요약

| RPC 타입 (`.proto` 정의)                       | 통신 방식                   | 실행되는 인터셉터 핸들러       |
| :--------------------------------------------- | :-------------------------- | :----------------------------- |
| `rpc Method(Req) returns (Res);`               | 💬 단일 요청 → 단일 응답    | `UnaryServerHandler`           |
| `rpc Method(stream Req) returns (Res);`        | 📤 스트림 요청 → 단일 응답  | `ClientStreamingServerHandler` |
| `rpc Method(Req) returns (stream Res);`        | 📥 단일 요청 → 스트림 응답  | `ServerStreamingServerHandler` |
| `rpc Method(stream Req) returns (stream Res);` | ⇄ 스트림 요청 ↔ 스트림 응답 | `DuplexStreamingServerHandler` |
