using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class GhostsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{guid}", GetGhost);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetGhost(Guid guid, IGhostService ghostService, HttpContext context, CancellationToken cancellationToken)
    {
        var data = await ghostService.GetGhostDataAsync(guid, cancellationToken);

        if (data is null)
        {
            return TypedResults.NotFound();
        }

        // CORS middleware is ???
        if (context.Request.Headers.ContainsKey(CorsConstants.Origin))
        {
            context.Response.Headers.AccessControlAllowOrigin = "https://3d.gbx.tools";
            context.Response.Headers.AccessControlAllowMethods = "GET, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "*";
        }

        return TypedResults.File(data.Data, "application/gbx", $"{guid}.Ghost.Gbx", lastModified: data.LastModifiedAt, entityTag: new EntityTagHeaderValue(data.Etag));
    }
}