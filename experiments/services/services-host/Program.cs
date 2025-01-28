var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.auth_service>("auth-service");

builder.Build().Run();
