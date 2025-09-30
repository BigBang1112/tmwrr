using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using TMWRR.Api;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class ReplaysEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{guid}", GetReplay);
        group.MapGet("/{guid}/download", DownloadReplay);
    }

    private static async Task<Results<Ok<Replay>, NotFound>> GetReplay(Guid guid, IReplayService replayService, CancellationToken cancellationToken)
    {
        var dto = await replayService.GetReplayDtoAsync(guid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadReplay(Guid guid, IReplayService replayService, HttpContext context, CancellationToken cancellationToken)
    {
        var data = await replayService.GetReplayDownloadAsync(guid, cancellationToken);

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

        return TypedResults.File(data.Data, "application/gbx", $"{guid}.Replay.Gbx", lastModified: data.LastModifiedAt, entityTag: new EntityTagHeaderValue(data.Etag));
    }
}