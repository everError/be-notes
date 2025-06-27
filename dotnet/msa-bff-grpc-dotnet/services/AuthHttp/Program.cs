using Auth.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=../Auth/users.db"));

var app = builder.Build();

app.MapControllers();
app.MapGet("/", () => "This server supports gRPC and REST endpoints.");
app.Run();
