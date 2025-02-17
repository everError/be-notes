using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.WebSockets;
using System.Text;

namespace auth_service.EndPoint;

public class WebSocketController : ControllerBase
{
    public WebSocketController() { }
    [Route("/ws")]
    [SwaggerIgnore]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Echo(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            // 받은 메시지 처리
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            Console.WriteLine($"클라이언트로부터 메시지 수신: {receivedMessage}");

            // 응답 메시지 결정
            string responseMessage = receivedMessage == "AA" ? "BB" : $"Echo: {receivedMessage}";
            byte[] responseBuffer = Encoding.UTF8.GetBytes(responseMessage);

            // 클라이언트에게 응답 전송
            await webSocket.SendAsync(
                new ArraySegment<byte>(responseBuffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            // 다음 메시지 수신
            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}
