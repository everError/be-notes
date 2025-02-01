var builder = DistributedApplication.CreateBuilder(args);

// auth-service의 인스턴스를 5001, 5002 포트에서 실행
builder.AddProject<Projects.auth_service>("auth-service-1")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5001");

builder.AddProject<Projects.auth_service>("auth-service-2")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5002");

// API Gateway 실행 (5000 포트에서 실행)
builder.AddProject<Projects.gateway_service>("gateway-service");

builder.Build().Run();
