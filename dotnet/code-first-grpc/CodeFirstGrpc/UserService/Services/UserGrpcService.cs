using Contracts.User;
using Grpc.Core;

namespace UserService.Services;

public class UserGrpcService : IUserGrpcService
{
    public ValueTask<UserDto> GetUserAsync(GetUserRequest request)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "INVALID_USER_ID")
            );
        }

        if (request.Id == 404)
        {
            throw new RpcException(
                new Status(StatusCode.NotFound, "USER_NOT_FOUND")
            );
        }

        return ValueTask.FromResult(new UserDto
        {
            Id = request.Id,
            Name = "홍길동"
        });
    }
}
