using Microsoft.AspNetCore.Http.HttpResults;

namespace TMWRR.Frontend.Endpoints;

public static class ViewEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("View");

        group.MapGet("/r/{guid}", ViewReplay);
        group.MapGet("/g/{guid}", ViewGhost);
        group.MapGet("/g/{guid}/{mapUid}", ViewGhostWithMapUid);
    }

    public static RedirectHttpResult ViewReplay(Guid guid)
    {
        return TypedResults.Redirect($"https://3d.gbx.tools/view/replay?url={Uri.EscapeDataString($"https://api.tmwrr.bigbang1112.cz/replays/{guid}/download")}");        
    }

    public static RedirectHttpResult ViewGhost(Guid guid)
    {
        return TypedResults.Redirect($"https://3d.gbx.tools/view/ghost?url={Uri.EscapeDataString($"https://api.tmwrr.bigbang1112.cz/ghosts/{guid}/download")}");
    }

    public static RedirectHttpResult ViewGhostWithMapUid(Guid guid, string mapUid)
    {
        return TypedResults.Redirect($"https://3d.gbx.tools/view/ghost?url={Uri.EscapeDataString($"https://api.tmwrr.bigbang1112.cz/ghosts/{guid}/download")}&mapuid={Uri.EscapeDataString(mapUid)}");
    }
}