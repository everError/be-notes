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
services.AddControllers(); // MVC 패턴의 컨트롤러를 사용할 수 있도록 등록
services.AddAuthorization()
    .AddEndpointsApiExplorer() // API 엔드포인트 문서를 자동으로 생성하는 서비스 추가
    .AddSwaggerGen(); //Swagger UI에서 API를 테스트할 수 있도록 지원

// Redis 연결
var redis = ConnectionMultiplexer.Connect("localhost");
services.AddSingleton<IConnectionMultiplexer>(redis);

var app = builder.Build();

// Redis Pub/Sub 구독 설정
var subscriber = redis.GetSubscriber();
subscriber.Subscribe(RedisChannel.Literal("refresh_token_events"), (channel, message) =>
{
    Console.WriteLine($"[Redis Pub/Sub] 이벤트 수신: {message}");
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // 개발 환경에서 Swagger JSON 문서 활성화
    app.UseSwaggerUI(); // Swagger UI를 통해 API 문서를 웹 UI로 확인 가능
}

app.UseRouting();
app.UseAuthentication()
    .UseAuthorization(); // JWT 인증 또는 기타 인증 미들웨어 적용
app.MapControllers();
app.UseWebSockets();

app.Run();