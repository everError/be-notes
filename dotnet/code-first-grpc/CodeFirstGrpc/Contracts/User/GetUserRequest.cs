using ProtoBuf;

namespace Contracts.User;

[ProtoContract]
public class GetUserRequest
{
    [ProtoMember(1)]
    public int Id { get; set; }
}
