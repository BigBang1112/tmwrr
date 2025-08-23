using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos;
using TMWRR.Services.TMF;

namespace TMWRR.Endpoints;

public class TMFCampaignsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetCampaigns);
        group.MapGet("/{id}", GetCampaign);
        group.MapGet("/{campaignId}/records/{mapUid}", GetCampaignRecordsByMapUid);
        group.MapGet("/{campaignId}/snapshots/latest", GetLatestCampaignSnapshot);
        group.MapGet("/{campaignId}/snapshots/{createdAt}/records/{mapUid}", GetCampaignSnapshotRecordsByMapUid);
    }

    private static async Task<Ok<IEnumerable<TMFCampaignDto>>> GetCampaigns(ICampaignService campaignService, CancellationToken cancellationToken)
    {
        var dtos = await campaignService.GetAllDtosAsync(cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFCampaignDto>, NotFound>> GetCampaign(string id, ICampaignService campaignService, CancellationToken cancellationToken)
    {
        var dto = await campaignService.GetDtoAsync(id, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Ok<IEnumerable<TMFCampaignScoresRecordDto>>> GetCampaignRecordsByMapUid(string campaignId, string mapUid, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        var dtos = await scoresSnapshotService.GetLatestRecordDtosAsync(campaignId, mapUid, cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFCampaignScoresSnapshotDto>, NotFound>> GetLatestCampaignSnapshot(string campaignId, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        var dto = await scoresSnapshotService.GetLatestSnapshotDtoAsync(campaignId, cancellationToken);
        
        if (dto is null)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(dto);
    }

    private static async Task<Ok<IEnumerable<TMFCampaignScoresRecordDto>>> GetCampaignSnapshotRecordsByMapUid(string campaignId, DateTimeOffset createdAt, string mapUid, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        var dtos = await scoresSnapshotService.GetSnapshotRecordDtosAsync(campaignId, createdAt, mapUid, cancellationToken);
        
        return TypedResults.Ok(dtos);
    }
}
