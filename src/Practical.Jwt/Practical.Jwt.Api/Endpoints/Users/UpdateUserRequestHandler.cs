using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Practical.Jwt.Api.Models;
using System.Threading.Tasks;

namespace Practical.Jwt.Api.Endpoints.Users;

public class UpdateUserRequestHandler : IEndpointHandler
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("api/users/{id}", async (UpdateUserRequestHandler handler, string id, UserModel model) =>
        {
            return await handler.HandleAsync(id, model);
        }).RequireAuthorization();
    }

    public Task<IResult> HandleAsync(string id, UserModel model)
    {
        model.Id = id;
        return Task.FromResult(Results.Ok(model));
    }
}
