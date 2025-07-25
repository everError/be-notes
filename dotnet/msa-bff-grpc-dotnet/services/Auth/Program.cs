using Auth.Data;
using Auth.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=users.db"));

var app = builder.Build();

app.MapGrpcService<GreeterService>();
app.MapGrpcService<UserService>();

app.MapGet("/", () => "This server supports gRPC and REST endpoints.");

app.Run();
