using Microsoft.AspNetCore.Routing;

namespace Practical.Jwt.Api.Endpoints;

public interface IEndpointHandler
{
    static abstract void MapEndpoint(IEndpointRouteBuilder endpoints);
}
