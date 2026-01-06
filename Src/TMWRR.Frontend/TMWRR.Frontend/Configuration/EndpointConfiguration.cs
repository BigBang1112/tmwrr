using TMWRR.Frontend.Endpoints;

namespace TMWRR.Frontend.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        ViewEndpoints.Map(app.MapGroup("v"));
        PlayerEndpoint.Map(app.MapGroup("p"));
        MapEndpoint.Map(app.MapGroup("m"));
    }
}