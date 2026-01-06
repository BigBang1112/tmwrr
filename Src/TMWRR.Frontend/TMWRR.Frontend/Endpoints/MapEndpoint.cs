using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace TMWRR.Frontend.Endpoints;

public static class MapEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Map");

        group.MapGet("/{mapUid}", RedirectToMaps);
    }

    // note the MAPS, this is because there can are allowed multiple maps with same MapUid
    public static Results<RedirectHttpResult, BadRequest<string>> RedirectToMaps([MaxLength(32)] string mapUid)
    {
        return TypedResults.Redirect($"https://ul.unitedascenders.xyz/leaderboards/tracks/{mapUid}");
    }
}
