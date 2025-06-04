using Microsoft.OpenApi.Models;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
var services = builder.Services;
var configuration = builder.Configuration;

configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
configuration.AddOcelotWithSwaggerSupport(options =>
{
    options.Folder = "configurations";
});

//Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 시스템 로그 줄이기
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Information() // 기본 레벨은 Information
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

services.AddConnections();
services.AddOcelot();
services.AddSwaggerForOcelot(configuration);
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
});

var app = builder.Build();

app.UseSwaggerForOcelotUI(options =>
{
    options.PathToSwaggerGenerator = "/swagger/docs";
});
app.UseWebSockets();
app.Use(async (context, next) =>
{
    var request = context.Request;
    Log.Information("➡️ HTTP {Method} {Path} | Query: {QueryString} | IP: {IP}",
        request.Method,
        request.Path,
        request.QueryString,
        context.Connection.RemoteIpAddress?.ToString());

    await next();

    Log.Information("⬅️ HTTP {StatusCode} {Method} {Path}",
        context.Response.StatusCode,
        request.Method,
        request.Path);
});

app.UseOcelot().Wait();

app.Run();
