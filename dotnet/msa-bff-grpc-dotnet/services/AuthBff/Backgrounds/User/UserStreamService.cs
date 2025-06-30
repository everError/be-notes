using Auth;
using Grpc.Core;

namespace AuthBff.Backgrounds.User;

public class UserStreamService(UserService.UserServiceClient client) : BackgroundService
{
    private readonly UserService.UserServiceClient _client = client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var call = _client.ChatUsersByName(cancellationToken: stoppingToken);

        // 스트림 핸들러 공유 저장
        ChatStreamBridge.RequestStream = call.RequestStream;

        _ = Task.Run(async () =>
        {
            await foreach (var response in call.ResponseStream.ReadAllAsync(stoppingToken))
            {
                ChatStreamBridge.ResponseQueue.Enqueue(response);
            }
        }, stoppingToken);

        // 그냥 살아만 있음
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
