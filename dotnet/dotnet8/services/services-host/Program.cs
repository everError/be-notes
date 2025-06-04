var builder = DistributedApplication.CreateBuilder(args);

// auth-service의 인스턴스를 5001, 5002 포트에서 실행
builder.AddProject<Projects.auth_service>("auth-service")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(name: "auth-service-http-1", port: 5001)
    .WithHttpEndpoint(name: "auth-service-http-2", port: 5002);

// API Gateway 실행 (5000 포트에서 실행) - 해당 프로젝트의 launchSettings
builder.AddProject<Projects.gateway_service>("gateway-service");

builder.AddProject<Projects.data_service>("data-service-1")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(name: "data-service-http-1", port: 5003)
    .WithHttpEndpoint(name: "data-service-http-2", port: 5004);

builder.Build().Run();
