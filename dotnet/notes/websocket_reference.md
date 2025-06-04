# ASP.NET WebSocket 클래스 및 메소드, 이벤트 정리

## 1. WebSocket 클래스 개요

`System.Net.WebSockets.WebSocket` 클래스는 클라이언트와 서버 간의 WebSocket 연결을 관리하는 역할을 합니다.

```csharp
using System.Net.WebSockets;
```

✅ **웹소켓을 사용하기 위해 필요한 네임스페이스**

---

## 2. 주요 메소드

### **🔹 AcceptWebSocketAsync**

```csharp
public Task<WebSocket> AcceptWebSocketAsync();
```

✅ **클라이언트의 WebSocket 연결 요청을 수락하는 메소드**

📌 **예제:**

```csharp
var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
```

### **🔹 ReceiveAsync**

```csharp
public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
```

✅ **클라이언트로부터 WebSocket 메시지를 비동기적으로 수신하는 메소드**

📌 **예제:**

```csharp
var buffer = new byte[1024 * 4];
var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
```

### **🔹 SendAsync**

```csharp
public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
```

✅ **서버가 클라이언트에게 메시지를 비동기적으로 전송하는 메소드**

📌 **예제:**

```csharp
var message = Encoding.UTF8.GetBytes("Hello WebSocket Client!");
await webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
```

### **🔹 CloseAsync**

```csharp
public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
```

✅ **WebSocket 연결을 닫는 메소드**

📌 **예제:**

```csharp
await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
```

---

## 3. WebSocket 상태 확인

`WebSocket`의 현재 상태를 확인하는 속성은 다음과 같습니다:

### **🔹 WebSocketState**

```csharp
public WebSocketState State { get; }
```

✅ **WebSocket의 현재 상태를 반환**

📌 **가능한 상태 값:**

- `WebSocketState.Connecting` → 연결 중
- `WebSocketState.Open` → 연결 완료
- `WebSocketState.CloseSent` → 닫힘 요청 전송
- `WebSocketState.CloseReceived` → 닫힘 요청 수신
- `WebSocketState.Closed` → 연결 종료됨
- `WebSocketState.Aborted` → 강제 종료됨

📌 **예제:**

```csharp
if (webSocket.State == WebSocketState.Open)
{
    Console.WriteLine("WebSocket 연결이 활성화됨");
}
```

---

## 4. WebSocket 이벤트 감지 예제

📌 **이벤트 감지 예제:**

```csharp
var buffer = new byte[1024 * 4];
WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

while (!receiveResult.CloseStatus.HasValue)
{
    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
    Console.WriteLine($"클라이언트 메시지: {receivedMessage}");
    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
}
```

✅ **`CloseStatus` 속성을 감지하여 WebSocket 종료 여부 확인**

---

## 5. WebSocket 서버 예제 (컨트롤러 기반)

📌 **WebSocket을 컨트롤러에서 처리하는 예제**

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;

[Route("/ws")]
[ApiController]
public class WebSocketController : ControllerBase
{
    [HttpGet]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketMessages(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private static async Task HandleWebSocketMessages(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            Console.WriteLine($"클라이언트 메시지 수신: {receivedMessage}");

            string responseMessage = "서버에서 응답: " + receivedMessage;
            byte[] responseBuffer = Encoding.UTF8.GetBytes(responseMessage);

            await webSocket.SendAsync(
                new ArraySegment<byte>(responseBuffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}
```

✅ **WebSocket을 컨트롤러 기반으로 구현하여 API 엔드포인트에서 관리 가능**

---

## 6. 결론

✔ `WebSocket`을 활용하면 **클라이언트와 서버 간의 실시간 양방향 통신이 가능**
✔ `ReceiveAsync`와 `SendAsync`를 활용하여 **메시지 수신 및 전송 가능**
✔ `WebSocketState`를 사용하여 **현재 연결 상태 확인 가능**
