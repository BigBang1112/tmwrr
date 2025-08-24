using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using TMWRR.Services.TMF;

namespace TMWRR.Endpoints;

public static class TMFReplaysEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{guid}", GetReplay);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetReplay(Guid guid, IReplayService replayService, CancellationToken cancellationToken)
    {
        var data = await replayService.GetReplayDataAsync(guid, cancellationToken);

        if (data is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.File(data.Data, "application/gbx", $"{guid}.Replay.Gbx", lastModified: data.LastModifiedAt, entityTag: new EntityTagHeaderValue(data.Etag));
    }
}