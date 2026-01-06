using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Api;

namespace TMWRR.Frontend.Endpoints;

public static class PlayerEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Player");

        group.MapGet("/{game}/{login}", RedirectToPlayer);
    }

    public static Results<RedirectHttpResult, BadRequest<string>> RedirectToPlayer(EGame game, string login)
    {
        if (game != EGame.TMF)
        {
            return TypedResults.BadRequest("Only TMF game is supported for checking a player.");
        }

        return TypedResults.Redirect($"https://ul.unitedascenders.xyz/lookup?login={login}");
    }
}
