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
        endpoints.MapPost("api/users", HandleAsync).RequireAuthorization();
    }

    private static Task<IResult> HandleAsync(UserModel model)
    {
        return Task.FromResult(Results.Ok(model));
    }
}
