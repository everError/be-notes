using auth_service.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = AccessTokenHandler.Instance.TokenValidationParameters;
    });
services.AddControllers(); // MVC ������ ��Ʈ�ѷ��� ����� �� �ֵ��� ���
services.AddAuthorization()
    .AddEndpointsApiExplorer() // API ��������Ʈ ������ �ڵ����� �����ϴ� ���� �߰�
    .AddSwaggerGen(); //Swagger UI���� API�� �׽�Ʈ�� �� �ֵ��� ����

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger(); // ���� ȯ�濡�� Swagger JSON ���� Ȱ��ȭ
    app.UseSwaggerUI(); // Swagger UI�� ���� API ������ �� UI�� Ȯ�� ����
//}

app.UseRouting();
app.UseAuthentication()
    .UseAuthorization(); // JWT ���� �Ǵ� ��Ÿ ���� �̵���� ����
app.MapControllers();

app.Run();