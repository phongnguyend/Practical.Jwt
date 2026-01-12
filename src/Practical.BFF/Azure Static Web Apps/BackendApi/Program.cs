using Microsoft.AspNetCore.Authentication.JwtBearer;
using BackendApi.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    builder.Configuration.GetSection("JwtBearer").Bind(options);
});

// Register OneSignal service
services.AddSingleton<IOneSignalService, OneSignalService>();

services.AddControllers(configure =>
{
})
.ConfigureApiBehaviorOptions(options =>
{
})
.AddJsonOptions(options =>
{
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
