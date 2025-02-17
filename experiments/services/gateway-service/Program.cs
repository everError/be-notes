using Microsoft.OpenApi.Models;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
configuration.AddOcelotWithSwaggerSupport(options =>
{
    options.Folder = "configurations";
});

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
app.UseOcelot().Wait();

app.Run();
