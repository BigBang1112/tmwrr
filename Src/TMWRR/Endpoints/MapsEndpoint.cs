using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Endpoints;

public static class MapsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{mapUid}", GetMap);
        group.MapGet("/{mapUid}/tmf/snapshots", GetTMFSnapshots);
    }

    private static async Task<Results<Ok<MapDto>, NotFound>> GetMap(string mapUid, IMapService mapService, CancellationToken cancellationToken)
    {
        var dto = await mapService.GetDtoAsync(mapUid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Ok<IEnumerable<TMFCampaignScoresSnapshotDto>>> GetTMFSnapshots(string mapUid, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        // TODO bad request when not a tmuf map

        var dtos = await scoresSnapshotService.GetAllSnapshotDtosAsync(mapUid, cancellationToken);

        return TypedResults.Ok(dtos);
    }
}