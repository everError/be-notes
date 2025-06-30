using Auth;
using AuthBff.Backgrounds.User;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace AuthBff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService.UserServiceClient grpcClient, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly UserService.UserServiceClient _grpcClient = grpcClient;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserApi");

    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] UserRequest request)
    {
        var result = await _grpcClient.AddUserAsync(request);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _grpcClient.GetUsersAsync(new Empty());
        return Ok(result.Users);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedUsers([FromQuery] int count = 100)
    {
        var result = await _grpcClient.SeedUsersAsync(new SeedUsersRequest { Count = count });
        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAllUsers()
    {
        await _httpClient.DeleteAsync("api/UserHttp");
        return Ok();
    }

    [HttpGet("stream")]
    public async IAsyncEnumerable<UserReply> StreamUsers()
    {
        using var call = _grpcClient.StreamUsers(new Empty());

        await foreach (var user in call.ResponseStream.ReadAllAsync())
        {
            yield return user;
        }
    }
    [HttpPost("send")]
    public async Task<IActionResult> SendName([FromBody] GetUserByNameRequest request)
    {
        if (ChatStreamBridge.RequestStream is null)
            return StatusCode(503, "Stream not ready");

        await ChatStreamBridge.RequestStream.WriteAsync(request);
        return Ok(new { status = "sent" });
    }

    [HttpGet("receive")]
    public IActionResult ReceiveResponse()
    {
        if (ChatStreamBridge.ResponseQueue.TryDequeue(out var reply))
            return Ok(reply);

        return NoContent();
    }
}
