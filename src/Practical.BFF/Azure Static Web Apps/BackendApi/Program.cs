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

// Register Azure Notification Hub service
services.AddSingleton<IAzureNotificationHubService, AzureNotificationHubService>();

// Register Firebase Cloud Messaging service
services.AddSingleton<IFirebaseService, FirebaseService>();

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
