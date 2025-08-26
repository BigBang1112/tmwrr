using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class GamesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetGames);
        group.MapGet("/{id}", GetGame);

        GamesTMFLoginsEndpoint.Map(group.MapGroup("tmf/logins"));
        GamesTMFCampaignsEndpoint.Map(group.MapGroup("tmf/campaigns"));
    }

    private static async Task<Ok<IEnumerable<GameDto>>> GetGames(IGameService gameService, CancellationToken cancellationToken)
    {
        var dtos = await gameService.GetAllDtosAsync(cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<GameDto>, ValidationProblem, NotFound>> GetGame(string id, IGameService gameService, CancellationToken cancellationToken)
    {
        if (id.Length > 12)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(id)] = ["The game ID length must not exceed 12 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await gameService.GetDtoAsync(id, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}
