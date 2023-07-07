using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Practical.Jwt.Api.Endpoints.Users;

public class DeleteUserRequestHandler : IEndpointHandler
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("api/users/{id}", HandleAsync).RequireAuthorization();
    }

    private static Task<IResult> HandleAsync(string id)
    {
        return Task.FromResult(Results.NoContent());
    }
}
