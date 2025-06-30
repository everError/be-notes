using Auth;
using Grpc.Core;
using System.Collections.Concurrent;

namespace AuthBff.Backgrounds.User;

public static class ChatStreamBridge
{
    public static IClientStreamWriter<GetUserByNameRequest>? RequestStream;
    public static ConcurrentQueue<GetUserByNameReply> ResponseQueue = new();
}

