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
    }

    private static async Task<Ok<IEnumerable<GameDto>>> GetGames(IGameService gameService, CancellationToken cancellationToken)
    {
        var dtos = await gameService.GetAllDtosAsync(cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<GameDto>, NotFound>> GetGame(string id, IGameService gameService, CancellationToken cancellationToken)
    {
        var dto = await gameService.GetDtoAsync(id, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}
