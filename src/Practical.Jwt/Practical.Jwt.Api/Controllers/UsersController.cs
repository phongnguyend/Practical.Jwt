using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Practical.Jwt.Api.Models;
using System.Collections.Generic;

namespace Practical.Jwt.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        public UsersController(IConfiguration configuration)
        {
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


    }
}
