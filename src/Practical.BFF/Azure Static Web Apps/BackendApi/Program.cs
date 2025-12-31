using Microsoft.AspNetCore.Authentication.JwtBearer;

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
