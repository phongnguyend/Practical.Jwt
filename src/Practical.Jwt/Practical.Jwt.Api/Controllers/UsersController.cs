using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Practical.Jwt.Api.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CryptographyHelper.HashAlgorithms;
using Practical.Jwt.Api.Entities;

namespace Practical.Jwt.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, RefreshToken> _refreshTokens = new Dictionary<string, RefreshToken>();
        private readonly IConfiguration _configuration;

        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public List<UserModel> Get()
        {
            return new List<UserModel>()
            {
                new UserModel
                {
                    Id = "1",
                },
                new UserModel
                {
                    Id = "2",
                }
            };
        }

        [HttpPost]
        public UserModel Post(UserModel model)
        {
            return model;
        }

        [HttpPut("{id}")]
        public UserModel Put(string id, UserModel model)
        {
            model.Id = id;
            return model;
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model != null)
            {
                model.UserName = model?.UserName?.ToLowerInvariant();
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToString()),
            };

            var token = CreateToken(authClaims);
            var refreshTokenPart1 = GenerateRefreshToken();
            var refreshTokenPart2 = GenerateRefreshToken();
            var refreshToken = $"{refreshTokenPart1}.{refreshTokenPart2}";

            lock (_lock)
            {
                _refreshTokens.Add(refreshTokenPart1, new RefreshToken
                {
                    UserName = model.UserName,
                    Expiration = DateTimeOffset.UtcNow.AddHours(24),
                    TokenHash = refreshToken.UseSha256().ComputeHashedString()
                });
            }

            return Ok(new
            {
                UserName = model.UserName,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expiration = token.ValidTo
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("refreshtoken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
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

            var token = CreateToken(authClaims);
            var newRefreshTokenPart1 = GenerateRefreshToken();
            var newRefreshTokenPart2 = GenerateRefreshToken();
            var newRefreshToken = $"{newRefreshTokenPart1}.{newRefreshTokenPart2}";

            lock (_lock)
            {
                _refreshTokens[refreshTokenPart1].ConsumedTime = DateTimeOffset.UtcNow;

                _refreshTokens.Add(newRefreshTokenPart1, new RefreshToken
                {
                    UserName = userName,
                    Expiration = DateTimeOffset.UtcNow.AddHours(24),
                    TokenHash = newRefreshToken.UseSha256().ComputeHashedString()
                });
            }

            return Ok(new
            {
                UserName = userName,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = newRefreshToken,
                Expiration = token.ValidTo
            });
        }

        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SymmetricKey"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddMinutes(5),
                claims: authClaims,
                signingCredentials: new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256));

            return token;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return randomNumber.UseSha256().ComputeHashedString();
        }

        private void ValidateToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SymmetricKey"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        }
    }
}
