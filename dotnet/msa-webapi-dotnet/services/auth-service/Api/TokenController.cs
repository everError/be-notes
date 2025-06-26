using auth_service.Attributes;
using auth_service.Handlers;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace auth_service.Api;

[ApiController]
[AuthRoute("token")]
public class TokenController(IConnectionMultiplexer redis) : ControllerBase
{
    private readonly IDatabase _redisDb = redis.GetDatabase();
    private readonly ISubscriber _redisPubSub = redis.GetSubscriber();

    /// <summary>
    /// Access Token 및 Refresh Token 발급 (Redis 저장 + Pub/Sub 이벤트)
    /// </summary>
    [HttpPost("access")]
    public IActionResult GenerateToken(string userName)
    {
        var accessToken = AccessTokenHandler.Instance.GenerateAccessToken(userName);
        var refreshToken = AccessTokenHandler.Instance.GenerateRefreshToken();

        // Redis에 Refresh Token 저장 (유효기간 7일 설정)
        _redisDb.StringSet($"refresh_token:{userName}", refreshToken, TimeSpan.FromDays(7));

        // Refresh Token 생성 이벤트 발행 (Pub/Sub)
        _redisPubSub.Publish(RedisChannel.Literal("refresh_token_events"), $"ADD:{userName}");

        // Refresh Token을 HttpOnly 쿠키에 저장
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { accessToken });
    }

    /// <summary>
    /// Refresh Token을 기반으로 새로운 Access Token 발급 (Redis 확인)
    /// </summary>
    [HttpPost("refresh")]
    public IActionResult RefreshAccessToken(string userName)
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            return Unauthorized(new { message = "Refresh Token 없음" });
        }

        var storedRefreshToken = _redisDb.StringGet($"refresh_token:{userName}");
        if (string.IsNullOrEmpty(storedRefreshToken) || storedRefreshToken != refreshToken)
        {
            return Unauthorized(new { message = "Refresh Token이 유효하지 않음" });
        }

        var newAccessToken = AccessTokenHandler.Instance.GenerateAccessToken(userName);
        return Ok(new { accessToken = newAccessToken });
    }

    /// <summary>
    /// 로그아웃 (Redis에서 Refresh Token 삭제 + Pub/Sub 이벤트)
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout(string userName)
    {
        _redisDb.KeyDelete($"refresh_token:{userName}"); // Redis에서 Refresh Token 삭제

        // Refresh Token 삭제 이벤트 발행 (Pub/Sub)
        _redisPubSub.Publish(RedisChannel.Literal("refresh_token_events"), $"DELETE:{userName}");

        Response.Cookies.Delete("refreshToken"); // 쿠키 삭제
        return Ok(new { message = "로그아웃 성공" });
    }
}
