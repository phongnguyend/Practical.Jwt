using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Practical.Jwt.Api.Models;
using System.Threading.Tasks;

namespace Practical.Jwt.Api.Endpoints.Users;

public class CreateUserRequestHandler : IEndpointHandler
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("api/users", async (CreateUserRequestHandler handler, UserModel model) =>
        {
            return await handler.HandleAsync(model);
        }).RequireAuthorization();
    }

    public Task<IResult> HandleAsync(UserModel model)
    {
        return Task.FromResult(Results.Ok(model));
    }
}
