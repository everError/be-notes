using System.ServiceModel;

namespace Contracts.User;

[ServiceContract]
public interface IUserGrpcService
{
    ValueTask<UserDto> GetUserAsync(GetUserRequest request);
}
