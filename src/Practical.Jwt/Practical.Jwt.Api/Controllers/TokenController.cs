using CryptographyHelper.HashAlgorithms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Practical.Jwt.Api.Entities;
using Practical.Jwt.Api.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Practical.Jwt.Api.Controllers
{
    [Route("connect/token")]
    [ApiController]
    public class TokenController : Controller
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, RefreshToken> _refreshTokens = new Dictionary<string, RefreshToken>();
        private readonly IConfiguration _configuration;

        public TokenController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult RequestToken([FromBody] TokenRequestModel model)
        {
            if (model != null)
            {
                model.UserName = model?.UserName?.ToLowerInvariant();
            }

            switch (model.GrantType)
            {
                case "password":
                    return GrantResourceOwnerCredentials(model);

                case "refresh_token":
                    return RefreshToken(model);

                //case "client_credentials":
                //    return GrantClientCredentials(model);

                default:
                    return BadRequest(new TokenResponseModel { Error = "unsupported_grant_type" });
            }


        }

        private IActionResult GrantResourceOwnerCredentials(TokenRequestModel model)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToString()),
            };

            var token = CreateToken(authClaims, DateTime.Now.AddMinutes(int.Parse(_configuration["Auth:AccessTokenLifetime:ResourceOwnerCredentials"])));
            var refreshTokenPart1 = GenerateRefreshToken();
            var refreshTokenPart2 = GenerateRefreshToken();
            var refreshToken = $"{refreshTokenPart1}.{refreshTokenPart2}";

            lock (_lock)
            {
                _refreshTokens.Add(refreshTokenPart1, new RefreshToken
                {
                    UserName = model.UserName,
                    Expiration = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_configuration["Auth:RefreshTokenLifetime:ResourceOwnerCredentials"])),
                    TokenHash = refreshToken.UseSha256().ComputeHashedString()
                });
            }

            return Ok(new TokenResponseModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expires = token.ValidTo
            });
        }

        private IActionResult RefreshToken([FromBody] TokenRequestModel model)
        {
            string refreshTokenPart1 = model.RefreshToken.Split('.')[0];

            if (!_refreshTokens.ContainsKey(refreshTokenPart1))
            {
                return BadRequest();
            }
            else if (_refreshTokens[refreshTokenPart1].ConsumedTime != null)
            {
                // TODO: logout and inform user
                return BadRequest();
            }
            else if (_refreshTokens[refreshTokenPart1].Expiration < DateTimeOffset.Now)
            {
                lock (_lock)
                {
                    _refreshTokens.Remove(refreshTokenPart1);
                }

                return BadRequest();
            }
            else if (_refreshTokens[refreshTokenPart1].TokenHash != model.RefreshToken.UseSha256().ComputeHashedString())
            {
                return BadRequest();
            }

            var userName = _refreshTokens[refreshTokenPart1].UserName;

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToString()),
            };

            var token = CreateToken(authClaims, DateTime.Now.AddMinutes(int.Parse(_configuration["Auth:AccessTokenLifetime:ResourceOwnerCredentials"])));
            var newRefreshTokenPart1 = GenerateRefreshToken();
            var newRefreshTokenPart2 = GenerateRefreshToken();
            var newRefreshToken = $"{newRefreshTokenPart1}.{newRefreshTokenPart2}";

            lock (_lock)
            {
                _refreshTokens[refreshTokenPart1].ConsumedTime = DateTimeOffset.UtcNow;

                _refreshTokens.Add(newRefreshTokenPart1, new RefreshToken
                {
                    UserName = userName,
                    Expiration = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_configuration["Auth:RefreshTokenLifetime:ResourceOwnerCredentials"])),
                    TokenHash = newRefreshToken.UseSha256().ComputeHashedString()
                });
            }

            return Ok(new TokenResponseModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = newRefreshToken,
                Expires = token.ValidTo
            });
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

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return randomNumber.UseSha256().ComputeHashedString();
        }

        private SecurityKey GetSigningKey()
        {
            if (!string.IsNullOrWhiteSpace(_configuration["Auth:Jwt:SigningSymmetricKey"]))
            {
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:Jwt:SigningSymmetricKey"]));
            }

            return new X509SecurityKey(new X509Certificate2(_configuration["Auth:Jwt:SigningCertificate:Path"], _configuration["Auth:Jwt:SigningCertificate:Password"]));
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
                var securityKey = new X509SecurityKey(new X509Certificate2(_configuration["Auth:Jwt:SigningCertificate:Path"], _configuration["Auth:Jwt:SigningCertificate:Password"]));
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
}
