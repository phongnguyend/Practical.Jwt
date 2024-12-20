using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Practical.Jwt.Api.Endpoints;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
var configuration = builder.Configuration;

var useMinimalApi = true;

if (useMinimalApi)
{
    services.AddAuthorization();
    services.AddCors();
}
else
{
    services.AddControllers();
}

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
});

// Configure the HTTP request pipeline.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors(configurePolicy =>
{
    configurePolicy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

if (useMinimalApi)
{
    var endpointHandlerTypes = Assembly.GetCallingAssembly()
    .GetTypes()
    .Where(x => x.GetInterfaces() != null && x.GetInterfaces().Contains(typeof(IEndpointHandler)))
    .ToList();

    foreach (var item in endpointHandlerTypes)
    {
        item.InvokeMember(nameof(IEndpointHandler.MapEndpoint), BindingFlags.InvokeMethod, null, null, new[] { app });
    }
}
else
{
    app.MapControllers();
}

app.Run();

static SecurityKey GetSigningKey(ConfigurationManager configuration)
{
    if (!string.IsNullOrWhiteSpace(configuration["Auth:Jwt:SigningSymmetricKey"]))
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:Jwt:SigningSymmetricKey"]));
    }

    return new X509SecurityKey(X509CertificateLoader.LoadPkcs12FromFile(configuration["Auth:Jwt:SigningCertificate:Path"], configuration["Auth:Jwt:SigningCertificate:Password"], X509KeyStorageFlags.EphemeralKeySet));
}
