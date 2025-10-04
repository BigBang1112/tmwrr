using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Api;
using TMWRR.Api.TMF;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Endpoints;

public static class MapEndpoints
{
    private static readonly DateTimeOffset lastModifiedAt = DateTimeOffset.UtcNow;

    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Map");

        group.MapGet("/", GetMaps)
            .WithSummary("Maps")
            .WithDescription("Search for maps by name. If no name is provided, returns a list of maps from the beginning.");

        group.MapGet("/{mapUid}", GetMap)
            .WithSummary("Map by UID")
            .WithDescription("Retrieve details of a specific map by its UID.");

        group.MapGet("/{mapUid}/thumbnail", GetMapThumbnail)
            .WithSummary("Map thumbnail")
            .WithDescription("Retrieve the thumbnail image for a specific map by its UID.");

        group.MapGet("/{mapUid}/tmf/snapshots", GetTMFSnapshots)
            .WithSummary("TMF campaign snapshots for map")
            .WithDescription("Retrieve TMF campaign score snapshots associated with a specific map by its UID.")
            .CacheOutput(CachePolicy.SnapshotsCampaignTMF);
    }

    private static async Task<Ok<IEnumerable<Map>>> GetMaps(string? name, int? offset, int? length, IMapService mapService, CancellationToken cancellationToken)
    {
        var dtos = await mapService.SearchDtosAsync(name ?? string.Empty, length ?? 25, offset ?? 0, cancellationToken);
        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<Map>, ValidationProblem, NotFound>> GetMap(
        string mapUid, 
        IMapService mapService, 
        CancellationToken cancellationToken)
    {
        if (mapUid.Length > 32)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(mapUid)] = ["The MapUid length must not exceed 32 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await mapService.GetDtoAsync(mapUid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetMapThumbnail(string mapUid, IMapService mapService, CancellationToken cancellationToken)
    {
        var thumbnail = await mapService.GetThumbnailAsync(mapUid, cancellationToken);

        if (thumbnail is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.File(thumbnail, "image/jpeg", lastModified: lastModifiedAt);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresSnapshot>>, ValidationProblem>> GetTMFSnapshots(string mapUid, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        if (mapUid.Length > 32)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(mapUid)] = ["The MapUid length must not exceed 32 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        // TODO bad request when not a tmuf map

        var dtos = await scoresSnapshotService.GetMapSnapshotDtosAsync(mapUid, cancellationToken);

        return TypedResults.Ok(dtos);
    }
}