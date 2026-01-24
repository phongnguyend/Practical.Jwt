using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Practical.MultipleAuthenticationSchemes.Api.Extensions;
using Practical.MultipleAuthenticationSchemes.Api.Services;
using System.Security.Cryptography.X509Certificates;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.

services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
services.AddOpenApi();

// Register services
services.AddScoped<IUserService, UserService>();
services.AddScoped<IApiKeyService, ApiKeyService>();

// Configure authentication with multiple schemes
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = configuration["Auth:Jwt:Issuer"],
        ValidAudience = configuration["Auth:Jwt:Audience"],
        IssuerSigningKey = GetSigningKey(configuration),
        ClockSkew = TimeSpan.FromSeconds(120) // default TimeSpan.FromSeconds(300)
    };
})
.AddBasic(options =>
{
    options.Realm = "Practical.MultipleAuthenticationSchemes.Api";
})
.AddApiKey(options =>
{
    options.HeaderName = "X-API-Key";
    options.Realm = "Practical.MultipleAuthenticationSchemes.Api";
});

// Configure authorization policies
services.AddAuthorization(options =>
{
    // Default policy accepts JWT, Basic, and API Key authentication
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme,
        "Basic",
        "ApiKey")
        .RequireAuthenticatedUser()
        .Build();

    // Policy that requires JWT authentication only
    options.AddPolicy("JwtOnly", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    // Policy that requires Basic authentication only
    options.AddPolicy("BasicOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("Basic");
        policy.RequireAuthenticatedUser();
    });

    // Policy that requires API Key authentication only
    options.AddPolicy("ApiKeyOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("ApiKey");
        policy.RequireAuthenticatedUser();
    });

    // Policy that accepts both JWT and API Key
    options.AddPolicy("JwtOrApiKey", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.AuthenticationSchemes.Add("ApiKey");
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static SecurityKey GetSigningKey(ConfigurationManager configuration)
{
    if (!string.IsNullOrWhiteSpace(configuration["Auth:Jwt:SigningSymmetricKey"]))
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:Jwt:SigningSymmetricKey"]));
    }

    return new X509SecurityKey(X509CertificateLoader.LoadPkcs12FromFile(configuration["Auth:Jwt:SigningCertificate:Path"], configuration["Auth:Jwt:SigningCertificate:Password"], X509KeyStorageFlags.EphemeralKeySet));
}
