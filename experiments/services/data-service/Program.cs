using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using data_service.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options
        .UseSqlite("Data Source=app.db")
        .AddInterceptors(new ConcurrencyVersionInterceptor());
});


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Data API",
        Version = "v1"
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var env = app.Services.GetRequiredService<IWebHostEnvironment>();
    if (env.IsDevelopment())
    {
        db.Database.Migrate(); // ✅ 개발 환경에서는 자동 마이그레이션
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Data API v1");
    });
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
