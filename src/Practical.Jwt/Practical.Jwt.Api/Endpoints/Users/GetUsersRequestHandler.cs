using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Practical.Jwt.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Practical.Jwt.Api.Endpoints.Users;

public class GetUsersRequestHandler : IEndpointHandler
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/users", async (GetUsersRequestHandler handler) =>
        {
            return await handler.HandleAsync();
        }).RequireAuthorization();
    }

    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok(new List<UserModel>()
        {
            new UserModel
            {
                Id = "1",
            },
            new UserModel
            {
                Id = "2",
            }
        }));
    }
}
