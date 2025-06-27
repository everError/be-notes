using Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcClient<UserService.UserServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5059");
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("UserApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5073");
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthBff API v1");
    c.RoutePrefix = "swagger";
});
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();