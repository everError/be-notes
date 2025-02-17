# WebSocket

## 개요

WebSocket은 클라이언트와 서버 간 **양방향, 실시간 통신**을 가능하게 하는 프로토콜입니다. 일반적인 HTTP 요청/응답 모델과 달리, WebSocket은 한 번의 연결 후 지속적으로 데이터를 주고받을 수 있습니다.

---

## WebSocket의 특징

1. **양방향 통신 (Full-Duplex)**
   - 클라이언트와 서버가 **동시에 데이터를 주고받을 수 있음**.
2. **지속적인 연결 (Persistent Connection)**

   - HTTP 요청-응답과 달리, 연결이 유지되며 필요할 때마다 데이터를 교환 가능.

3. **낮은 오버헤드 (Low Overhead)**

   - HTTP보다 가벼운 헤더 구조를 사용하여 **빠른 데이터 전송**이 가능.

4. **이벤트 기반 (Event-Driven)**

   - 클라이언트와 서버가 특정 이벤트 발생 시 데이터를 주고받을 수 있음.

5. **표준 프로토콜 지원**
   - 대부분의 브라우저와 서버에서 WebSocket을 지원함.

---

## WebSocket 동작 방식

1. **핸드셰이크 (Handshake) 과정**

   - 클라이언트가 서버에 `Upgrade` 헤더를 포함한 요청을 보내 WebSocket 연결을 요청.
   - 서버가 `101 Switching Protocols` 응답을 보내며 WebSocket 연결을 승인.

2. **데이터 송수신**

   - 연결이 설정되면, 클라이언트와 서버는 **양방향으로 데이터를 주고받을 수 있음**.

3. **연결 종료**
   - 클라이언트 또는 서버가 연결을 닫을 수 있음.
   - 비정상적인 종료 방지를 위해 `Close` 프레임을 송수신하여 정상적으로 종료할 수 있음.

---

## WebSocket과 HTTP 비교

| 비교 항목        | HTTP                      | WebSocket                   |
| ---------------- | ------------------------- | --------------------------- |
| 통신 방식        | 요청/응답 기반            | 양방향 실시간 통신          |
| 연결 유지        | 요청마다 새 연결          | 지속적인 연결 유지          |
| 데이터 전송 효율 | 헤더 포함한 요청마다 전송 | 최소한의 헤더로 효율적      |
| 사용 사례        | 정적 웹사이트, API 요청   | 채팅, 실시간 알림, 스트리밍 |

---

## WebSocket 사용 사례

- **실시간 채팅 (Chat Application)**
- **주식/코인 거래 시스템 (Stock & Crypto Trading)**
- **온라인 게임 (Online Gaming)**
- **IoT 데이터 스트리밍**
- **라이브 스포츠 중계**
- **멀티플레이 협업 앱 (Google Docs, Figma 등)**

---

## WebSocket의 한계

❌ **프록시 및 방화벽 문제** → 일부 네트워크 환경에서는 WebSocket이 차단될 수 있음.
❌ **서버 부하 증가** → 지속적인 연결을 유지해야 하므로 많은 클라이언트가 연결되면 서버 부담 증가.
❌ **브라우저 및 네트워크 지원 제한** → 일부 오래된 브라우저나 보안 정책이 엄격한 환경에서는 WebSocket을 사용할 수 없음.

---

## WebSocket 예제 코드

### **JavaScript 클라이언트 예제**

```javascript
const socket = new WebSocket("ws://localhost:8080");

// 연결 성공 시
socket.onopen = () => {
  console.log("WebSocket 연결 성공");
  socket.send("Hello Server!");
};

// 서버로부터 메시지 수신 시
socket.onmessage = (event) => {
  console.log(`서버로부터 메시지 수신: ${event.data}`);
};

// 연결 종료 시
socket.onclose = () => {
  console.log("WebSocket 연결 종료");
};
```

### **.NET 8 WebSocket 서버 예제**

```csharp
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await Echo(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

async Task Echo(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Text)
        {
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            byte[] responseBuffer = Encoding.UTF8.GetBytes($"Echo: {receivedMessage}");
            await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }
}

app.Run();
```

---

## 결론

WebSocket은 **양방향, 실시간 통신**이 필요한 애플리케이션에 적합한 프로토콜입니다. HTTP보다 **데이터 전송 효율성이 높고, 지속적인 연결 유지가 가능**하므로, **실시간 서비스 구현에 필수적인 기술**입니다. 하지만 서버 부하 및 방화벽 이슈를 고려해야 하며, 네트워크 환경에 따라 적절한 대체 기술(SSE, gRPC-Web 등)과 함께 사용될 수도 있습니다.
