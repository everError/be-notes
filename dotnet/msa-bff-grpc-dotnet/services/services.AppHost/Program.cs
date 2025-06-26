var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Auth>("auth");

builder.Build().Run();
