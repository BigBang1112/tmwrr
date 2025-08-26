using TMWRR.Endpoints;

namespace TMWRR.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Welcome to TMWRR! Beware this API version is EXPERIMENTAL and there will be breaking changes."
            });
        });

        GhostsEndpoint.Map(app.MapGroup("ghosts"));
        GamesEndpoint.Map(app.MapGroup("games"));
        EnvironmentsEndpoint.Map(app.MapGroup("environments"));
        UsersEndpoint.Map(app.MapGroup("users"));
        MapsEndpoint.Map(app.MapGroup("maps"));
    }
}