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

    private static async Task<Results<Ok<TMFCampaignDto>, ValidationProblem, NotFound>> GetCampaign(string id, ICampaignService campaignService, CancellationToken cancellationToken)
    {
        if (id.Length > 32)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(id)] = ["The campaign ID length must not exceed 32 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await campaignService.GetDtoAsync(id, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresRecordDto>>, ValidationProblem>> GetCampaignRecordsByMapUid(string campaignId, string mapUid, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (campaignId.Length > 32)
        {
            errors[nameof(campaignId)] = ["The campaign ID length must not exceed 32 characters."];
        }
        if (mapUid.Length > 32)
        {
            errors[nameof(mapUid)] = ["The MapUid length must not exceed 32 characters."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var dtos = await scoresSnapshotService.GetLatestRecordDtosAsync(campaignId, mapUid, cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFCampaignScoresSnapshotDto>, ValidationProblem, NotFound>> GetLatestCampaignSnapshot(string campaignId, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        if (campaignId.Length > 32)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(campaignId)] = ["The campaign ID length must not exceed 32 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await scoresSnapshotService.GetLatestSnapshotDtoAsync(campaignId, cancellationToken);
        
        if (dto is null)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresRecordDto>>, ValidationProblem>> GetCampaignSnapshotRecordsByMapUid(string campaignId, DateTimeOffset createdAt, string mapUid, IScoresSnapshotService scoresSnapshotService, CancellationToken cancellationToken)
    {
        if (campaignId.Length > 32)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(campaignId)] = ["The campaign ID length must not exceed 32 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dtos = await scoresSnapshotService.GetSnapshotRecordDtosAsync(campaignId, createdAt, mapUid, cancellationToken);
        
        return TypedResults.Ok(dtos);
    }
}
