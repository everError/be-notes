using Contracts.User;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Bff.Api;

[ApiController]
[Route("users")]
public class UserController(IUserGrpcService client) : ControllerBase
{
    private readonly IUserGrpcService _client  = client;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var user =  await _client.GetUserAsync(
                new GetUserRequest { Id = id }
            );

            return Ok(user);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            throw new Exception("BFF: 사용자 없음");
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
        {
            throw new Exception("BFF: 잘못된 요청");
        }
    }
}
