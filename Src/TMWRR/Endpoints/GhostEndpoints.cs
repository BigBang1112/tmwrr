using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using TMWRR.Api;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class GhostEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Ghost");

        group.MapGet("/{guid}", GetGhost)
            .WithSummary("Ghost by GUID")
            .WithDescription("Retrieve details of a specific ghost by its GUID.");

        group.MapGet("/{guid}/download", DownloadGhost)
            .WithSummary("Download ghost")
            .WithDescription("Download the ghost file associated with the specified GUID.");
    }

    private static async Task<Results<Ok<Ghost>, NotFound>> GetGhost(Guid guid, IGhostService ghostService, CancellationToken cancellationToken)
    {
        var dto = await ghostService.GetGhostDtoAsync(guid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadGhost(Guid guid, IGhostService ghostService, HttpContext context, CancellationToken cancellationToken)
    {
        var data = await ghostService.GetGhostDownloadAsync(guid, cancellationToken);

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