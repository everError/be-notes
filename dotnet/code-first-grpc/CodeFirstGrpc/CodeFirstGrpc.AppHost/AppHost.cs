var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.UserService>("userservice");

builder.AddProject<Projects.Bff>("bff");

builder.Build().Run();
