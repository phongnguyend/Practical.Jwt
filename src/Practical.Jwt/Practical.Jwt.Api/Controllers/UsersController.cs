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

namespace Practical.Jwt.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, (string Token, DateTimeOffset Expiration)> _refreshTokens = new Dictionary<string, (string Token, DateTimeOffset Expiration)>();
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
            var refreshToken = GenerateRefreshToken();

            lock (_lock)
            {
                _refreshTokens[model.UserName] = (refreshToken, DateTimeOffset.UtcNow.AddHours(24));
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
            if (model != null)
            {
                model.UserName = model?.UserName?.ToLowerInvariant();
            }

            var validRefreshToken = _refreshTokens.ContainsKey(model.UserName)
                && _refreshTokens[model.UserName].Token == model.RefreshToken
                && _refreshTokens[model.UserName].Expiration > DateTimeOffset.Now;

            if (!validRefreshToken)
            {
                return BadRequest();
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToString()),
            };

            var token = CreateToken(authClaims);
            var refreshToken = GenerateRefreshToken();

            lock (_lock)
            {
                _refreshTokens[model.UserName] = (refreshToken, DateTimeOffset.UtcNow.AddHours(24));
            }

            return Ok(new
            {
                UserName = model.UserName,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
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
            return Convert.ToBase64String(randomNumber);
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
