using TMWRR.Api;
using TMWRR.Endpoints;

namespace TMWRR.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () =>
        {
            return new TmwrrInformation
            {
                Message = "Welcome to TMWRR! Beware this API version is EXPERIMENTAL and there will be breaking changes."
            };
        })
            .WithTags("TMWRR")
            .WithSummary("Welcome");

        GhostEndpoints.Map(app.MapGroup("ghosts"));
        GameEndpoints.Map(app.MapGroup("games"));
        EnvironmentEndpoints.Map(app.MapGroup("environments"));
        UserEndpoints.Map(app.MapGroup("users"));
        Endpoints.MapEndpoints.Map(app.MapGroup("maps"));
        ReplayEndpoints.Map(app.MapGroup("replays"));
    }
}