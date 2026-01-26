using Contracts.User;
using ProtoBuf.Grpc.Reflection;

var generator = new SchemaGenerator();

// gRPC 서비스 기준으로 proto 생성
var schema = generator.GetSchema<IUserGrpcService>();

File.WriteAllText("user.proto", schema);