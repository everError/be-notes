using auth_service.Attributes;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace auth_service.Api;

[ApiController]
[AuthRoute("redis")]
public class RedisController(IConnectionMultiplexer redis) : ControllerBase
{
    private readonly IDatabase _redisDb = redis.GetDatabase();

    /// <summary>
    /// Redis에 저장된 모든 Refresh Token 조회
    /// </summary>
    [HttpGet("redis-data")]
    public IActionResult GetAllRefreshTokens()
    {
        var server = _redisDb.Multiplexer.GetServer("localhost", 6379);
        var keys = server.Keys(pattern: "refresh_token:*").ToList();

        var tokens = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            tokens[key.ToString()] = _redisDb.StringGet(key);
        }

        return Ok(tokens);
    }
}
