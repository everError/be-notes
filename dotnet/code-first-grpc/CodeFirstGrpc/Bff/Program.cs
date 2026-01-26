using Contracts.User;
using ProtoBuf.Grpc.ClientFactory;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// gRPC client µî·Ï
builder.Services.AddCodeFirstGrpcClient<IUserGrpcService>(o =>
{
    o.Address = new Uri("http://localhost:5175");
});

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();