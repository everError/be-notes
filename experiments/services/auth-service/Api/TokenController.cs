using auth_service.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Api;

[Route("api/token")]
[ApiController]
public class TokenController : ControllerBase
{
    public TokenController() { }

    [HttpPost("access")]
    public IActionResult GenerateToken(string userName) => Ok(new { token = AccessTokenHandler.Instance.GenerateAccessToken(userName) });
    [HttpGet("validate")]
    public IActionResult ValidateToken(string token) => Ok(new { Result = AccessTokenHandler.Instance.ValidateToken(token) });
}
