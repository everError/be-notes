using auth_service.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = AccessTokenHandler.Instance.TokenValidationParameters;
    });
services.AddControllers(); // MVC ������ ��Ʈ�ѷ��� ����� �� �ֵ��� ���
services.AddAuthorization()
    .AddEndpointsApiExplorer() // API ��������Ʈ ������ �ڵ����� �����ϴ� ���� �߰�
    .AddSwaggerGen(); //Swagger UI���� API�� �׽�Ʈ�� �� �ֵ��� ����

// Redis ����
var redis = ConnectionMultiplexer.Connect("localhost");
services.AddSingleton<IConnectionMultiplexer>(redis);

var app = builder.Build();

// Redis Pub/Sub ���� ����
var subscriber = redis.GetSubscriber();
subscriber.Subscribe(RedisChannel.Literal("refresh_token_events"), (channel, message) =>
{
    Console.WriteLine($"[Redis Pub/Sub] �̺�Ʈ ����: {message}");
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // ���� ȯ�濡�� Swagger JSON ���� Ȱ��ȭ
    app.UseSwaggerUI(); // Swagger UI�� ���� API ������ �� UI�� Ȯ�� ����
}

app.UseRouting();
app.UseAuthentication()
    .UseAuthorization(); // JWT ���� �Ǵ� ��Ÿ ���� �̵���� ����
app.MapControllers();
app.UseWebSockets();

app.Run();