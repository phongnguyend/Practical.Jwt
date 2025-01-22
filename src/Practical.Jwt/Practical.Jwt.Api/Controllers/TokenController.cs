using CryptographyHelper.HashAlgorithms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Practical.Jwt.Api.Entities;
using Practical.Jwt.Api.Extensions;
using Practical.Jwt.Api.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Practical.Jwt.Api.Controllers;

[Route("connect/token")]
[ApiController]
public class TokenController : Controller
{
    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private static readonly Dictionary<string, RefreshToken> _refreshTokens = new Dictionary<string, RefreshToken>();
    private readonly IConfiguration _configuration;

    public TokenController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> RequestToken()
    {
        var model = new TokenRequestModel
        {
            GrantType = Request.Form["grant_type"],
            UserName = Request.Form["username"],
            Password = Request.Form["password"],
            ClientId = Request.Form["client_id"],
            ClientSecret = Request.Form["client_secret"],
            RefreshToken = Request.Form["refresh_token"],
            Scope = Request.Form["scope"],
        };

        if (model != null)
        {
            model.UserName = model?.UserName?.ToLowerInvariant();
        }

        switch (model.GrantType)
        {
            case "password":
                return await GrantResourceOwnerCredentialsAsync(model);

            case "refresh_token":
                return await RefreshTokenAsync(model);

            case "client_credentials":
                return await GrantClientCredentialsAsync(model);

            default:
                return BadRequest(new TokenResponseModel { Error = "unsupported_grant_type" });
        }


    }

    private async Task<IActionResult> GrantResourceOwnerCredentialsAsync(TokenRequestModel model)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        };

        var token = CreateToken(authClaims, DateTime.Now.AddMinutes(int.Parse(_configuration["Auth:AccessTokenLifetime:ResourceOwnerCredentials"])));

        var refreshTokenBytes = GenerateRefreshToken();
        var refreshToken = Convert.ToHexString(refreshTokenBytes);
        var refreshTokenHash = Convert.ToHexString(refreshTokenBytes.UseSha256().ComputeHash());

        await _lock.WaitAsync();
        try
        {
            _refreshTokens.Add(refreshTokenHash, new RefreshToken
            {
                UserName = model.UserName,
                Expiration = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_configuration["Auth:RefreshTokenLifetime:ResourceOwnerCredentials"])),
            });
        }
        finally
        {
            _lock.Release();
        }

        return Ok(new TokenResponseModel
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            Expires = token.ValidTo
        });
    }

    private async Task<IActionResult> GrantClientCredentialsAsync(TokenRequestModel model)
    {
        string? clientId;
        string? clientSecret;
        if (!Request.TryGetBasicCredentials(out clientId, out clientSecret))
        {
            clientId = Request.Form["client_id"];
            clientSecret = Request.Form["client_secret"];
        }

        var authClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim("grant_type", "client_credentials"),
            new Claim("client_id", clientId),
        };

        var token = CreateToken(authClaims, DateTime.Now.AddMinutes(int.Parse(_configuration["Auth:AccessTokenLifetime:ClientCredentials"])));

        return Ok(new TokenResponseModel
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            Expires = token.ValidTo
        });

    }

    private async Task<IActionResult> RefreshTokenAsync([FromBody] TokenRequestModel model)
    {
        await _lock.WaitAsync();
        try
        {
            var refreshTokenBytes = Convert.FromHexString(model.RefreshToken);
            var refreshTokenHash = Convert.ToHexString(refreshTokenBytes.UseSha256().ComputeHash());

            if (!_refreshTokens.ContainsKey(refreshTokenHash))
            {
                return BadRequest();
            }
            else if (_refreshTokens[refreshTokenHash].ConsumedTime != null)
            {
                // TODO: logout and inform user
                return BadRequest();
            }
            else if (_refreshTokens[refreshTokenHash].Expiration < DateTimeOffset.Now)
            {
                return BadRequest();
            }

            var userName = _refreshTokens[refreshTokenHash].UserName;

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            };

            var token = CreateToken(authClaims, DateTime.Now.AddMinutes(int.Parse(_configuration["Auth:AccessTokenLifetime:ResourceOwnerCredentials"])));

            var newRefreshTokenBytes = GenerateRefreshToken();
            var newRefreshToken = Convert.ToHexString(newRefreshTokenBytes);
            var newRefreshTokenHash = Convert.ToHexString(newRefreshTokenBytes.UseSha256().ComputeHash());

            _refreshTokens[refreshTokenHash].ConsumedTime = DateTimeOffset.UtcNow;

            _refreshTokens.Add(newRefreshTokenHash, new RefreshToken
            {
                UserName = userName,
                Expiration = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_configuration["Auth:RefreshTokenLifetime:ResourceOwnerCredentials"])),
            });

            return Ok(new TokenResponseModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = newRefreshToken,
                Expires = token.ValidTo
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    private JwtSecurityToken CreateToken(List<Claim> authClaims, DateTime expires)
    {
        var token = new JwtSecurityToken(
            issuer: _configuration["Auth:Jwt:Issuer"],
            audience: _configuration["Auth:Jwt:Audience"],
            expires: expires,
            claims: authClaims,
            signingCredentials: GetSigningCredentials());

        return token;
    }

    private static byte[] GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return randomNumber;
    }

    private SecurityKey GetSigningKey()
    {
        if (!string.IsNullOrWhiteSpace(_configuration["Auth:Jwt:SigningSymmetricKey"]))
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:Jwt:SigningSymmetricKey"]));
        }

        return new X509SecurityKey(X509CertificateLoader.LoadPkcs12FromFile(_configuration["Auth:Jwt:SigningCertificate:Path"], _configuration["Auth:Jwt:SigningCertificate:Password"], X509KeyStorageFlags.EphemeralKeySet));
    }

    private SigningCredentials GetSigningCredentials()
    {
        if (!string.IsNullOrWhiteSpace(_configuration["Auth:Jwt:SigningSymmetricKey"]))
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:Jwt:SigningSymmetricKey"]));
            return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        }
        else
        {
            var securityKey = new X509SecurityKey(X509CertificateLoader.LoadPkcs12FromFile(_configuration["Auth:Jwt:SigningCertificate:Path"], _configuration["Auth:Jwt:SigningCertificate:Password"], X509KeyStorageFlags.EphemeralKeySet));
            return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        }
    }

    private void ValidateToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _configuration["Auth:Jwt:Issuer"],
            ValidAudience = _configuration["Auth:Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
    }
}
