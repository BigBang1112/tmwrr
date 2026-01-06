using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using TMWRR.Api;

namespace TMWRR.Frontend.Endpoints;

public static class ViewEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("View");

        group.MapGet("/r/{guidOrEncoded}", ViewReplay)
            .WithSummary("View replay")
            .WithDescription("Redirects to 3D viewer for a replay. Accepts either a standard GUID or base64 encoded GUID.");

        group.MapGet("/g/{guidOrEncoded}", ViewGhost)
            .WithSummary("View ghost")
            .WithDescription("Redirects to 3D viewer for a ghost. Accepts either a standard GUID or base64 encoded GUID.");

        group.MapGet("/g/{guidOrEncoded}/{mapUid}", ViewGhostWithMapUid)
            .WithSummary("View ghost with map")
            .WithDescription("Redirects to 3D viewer for a ghost with specific map context. Accepts either a standard GUID or base64 encoded GUID.");
    }

    public static Results<RedirectHttpResult, BadRequest<string>> ViewReplay([MaxLength(36)] string guidOrEncoded)
    {
        if (!GuidHelpers.TryParseOrEncoded(guidOrEncoded, out var guid))
        {
            return TypedResults.BadRequest("Invalid GUID format. Expected either a standard GUID or base64 encoded GUID.");
        }

        return TypedResults.Redirect($"https://3d.gbx.tools/view/replay?url={Uri.EscapeDataString($"https://api.tmwrr.bigbang1112.cz/replays/{guid}/download")}");        
    }

    public static Results<RedirectHttpResult, BadRequest<string>> ViewGhost([MaxLength(36)] string guidOrEncoded)
    {
        if (!GuidHelpers.TryParseOrEncoded(guidOrEncoded, out var guid))
        {
            return TypedResults.BadRequest("Invalid GUID format. Expected either a standard GUID or base64 encoded GUID.");
        }

        return TypedResults.Redirect($"https://3d.gbx.tools/view/ghost?url={Uri.EscapeDataString($"https://api.tmwrr.bigbang1112.cz/ghosts/{guid}/download")}");
    }

    public static Results<RedirectHttpResult, BadRequest<string>> ViewGhostWithMapUid([MaxLength(36)] string guidOrEncoded, [MaxLength(36)] string mapUid)
    {
        if (!GuidHelpers.TryParseOrEncoded(guidOrEncoded, out var guid))
        {
            return TypedResults.BadRequest("Invalid GUID format. Expected either a standard GUID or base64 encoded GUID.");
        }

        return TypedResults.Redirect($"https://3d.gbx.tools/view/ghost?url={Uri.EscapeDataString($"https://api.tmwrr.bigbang1112.cz/ghosts/{guid}/download")}&mapuid={Uri.EscapeDataString(mapUid)}");
    }

    public static Results<RedirectHttpResult, BadRequest<string>> ViewPlayer(EGame game, string login)
    {
        if (game != EGame.TMF)
        {
            return TypedResults.BadRequest("Only TMF game is supported for checking a player.");
        }

        return TypedResults.Redirect($"https://ul.unitedascenders.xyz/lookup?login={login}");
    }
}