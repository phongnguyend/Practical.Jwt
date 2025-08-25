using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Microsoft Entra ID authentication with enhanced debugging
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        // Enhanced token validation debugging
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                Console.WriteLine("=== TOKEN VALIDATION SUCCESS ===");
                Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                Console.WriteLine($"Claims count: {context.Principal?.Claims?.Count()}");
                
                var audienceClaim = context.Principal?.FindFirst("aud");
                var issuerClaim = context.Principal?.FindFirst("iss");
                var appIdClaim = context.Principal?.FindFirst("appid");
                
                Console.WriteLine($"Audience (aud): {audienceClaim?.Value}");
                Console.WriteLine($"Issuer (iss): {issuerClaim?.Value}");
                Console.WriteLine($"App ID (appid): {appIdClaim?.Value}");
                Console.WriteLine("================================");
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("=== TOKEN VALIDATION FAILED ===");
                Console.WriteLine($"Error: {context.Exception?.Message}");
                Console.WriteLine($"Exception Type: {context.Exception?.GetType().Name}");
                
                if (context.Exception?.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {context.Exception.InnerException.Message}");
                }
                
                // Log the token for debugging (remove in production!)
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    Console.WriteLine($"Failed Token: {token}");
                    Console.WriteLine("💡 Decode this token at https://jwt.ms to inspect claims");
                }
                
                Console.WriteLine("===============================");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("=== AUTHENTICATION CHALLENGE ===");
                Console.WriteLine($"AuthenticateFailure: {context.AuthenticateFailure?.Message}");
                Console.WriteLine($"Error: {context.Error}");
                Console.WriteLine($"Error Description: {context.ErrorDescription}");
                Console.WriteLine("=================================");
                return Task.CompletedTask;
            }
        };
    }, options =>
    {
        builder.Configuration.GetSection("AzureAd").Bind(options);
        
        // Debug configuration
        Console.WriteLine("=== API CONFIGURATION DEBUG ===");
        Console.WriteLine($"Instance: {options.Instance}");
        Console.WriteLine($"TenantId: {options.TenantId}");
        Console.WriteLine($"ClientId: {options.ClientId}");
        Console.WriteLine($"Domain: {options.Domain}");
        //Console.WriteLine($"Audience: {options.Audience}");
        Console.WriteLine("===============================");
    });

builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
