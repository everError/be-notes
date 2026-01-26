using ProtoBuf;

namespace Contracts.User;

[ProtoContract]
public class UserDto
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = default!;
}
