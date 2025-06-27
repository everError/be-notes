var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Auth>("auth");

builder.AddProject<Projects.AuthBff>("authbff");

builder.AddProject<Projects.AuthHttp>("authhttp");

builder.Build().Run();
