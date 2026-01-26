using ProtoBuf.Grpc.Server;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddCodeFirstGrpc();

var app = builder.Build();

app.MapGrpcService<UserGrpcService>();
app.MapGet("/", () => "User gRPC Service");

app.Run();
