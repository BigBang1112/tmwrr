using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos;
using TMWRR.Dtos.TMF;
using TMWRR.Enums;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Endpoints;

public static class GamesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetGames);
        group.MapGet("/{gameId}", GetGame);

        group.MapGet("/{gameId}/campaigns", GetGameCampaigns);
        group.MapGet("/{gameId}/campaigns/{campaignId}", GetGameCampaign);
        group.MapGet("/{gameId}/campaigns/{campaignId}/maps", GetGameCampaignMaps);
        group.MapGet("/{gameId}/campaigns/{campaignId}/maps/{mapUid}", GetGameCampaignMap);
        group.MapGet("/{gameId}/campaigns/{campaignId}/maps/{mapUid}/records", GetGameCampaignRecordsByMapUid);
        group.MapGet("/{gameId}/campaigns/{campaignId}/snapshots/latest", GetLatestGameCampaignSnapshot);
        group.MapGet("/{gameId}/campaigns/{campaignId}/snapshots/{createdAt}/{mapUid}/records", GetGameCampaignSnapshotRecordsByMapUid);

        group.MapGet("/{gameId}/logins/{loginId}", GetGameLogin);
    }

    private static async Task<Ok<IEnumerable<GameDto>>> GetGames(IGameService gameService, CancellationToken cancellationToken)
    {
        var dtos = await gameService.GetAllDtosAsync(cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<GameDto>, ValidationProblem, NotFound>> GetGame(
        EGame gameId, 
        IGameService gameService, 
        CancellationToken cancellationToken)
    {
        var dto = await gameService.GetDtoAsync(gameId, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignDto>>, ValidationProblem>> GetGameCampaigns(
        EGame gameId, 
        ICampaignService campaignService, 
        CancellationToken cancellationToken)
    {
        if (gameId != EGame.TMF)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(gameId)] = ["Only 'TMF' is supported as game ID."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dtos = await campaignService.GetAllDtosAsync(cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFCampaignDto>, ValidationProblem, NotFound>> GetGameCampaign(
        EGame gameId, 
        string campaignId,
        ICampaignService campaignService, 
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (gameId != EGame.TMF)
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
        if (campaignId.Length > 32)
        {
            errors[nameof(campaignId)] = ["The campaign ID length must not exceed 32 characters."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await campaignService.GetDtoAsync(campaignId, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignMapDto>>, ValidationProblem>> GetGameCampaignMaps(
        EGame gameId, 
        string campaignId, 
        ICampaignService campaignService, 
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (gameId != EGame.TMF)
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
        if (campaignId.Length > 32)
        {
            errors[nameof(campaignId)] = ["The campaign ID length must not exceed 32 characters."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var dtos = await campaignService.GetMapDtosAsync(campaignId, cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFCampaignMapDto>, ValidationProblem, NotFound>> GetGameCampaignMap(
        EGame gameId, 
        string campaignId, 
        string mapUid, 
        ICampaignService campaignService, 
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (gameId != EGame.TMF)
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
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

        var dto = await campaignService.GetMapDtoAsync(campaignId, mapUid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresRecordDto>>, ValidationProblem>> GetGameCampaignRecordsByMapUid(
        EGame gameId,
        string campaignId, 
        string mapUid,
        IScoresSnapshotService scoresSnapshotService,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (gameId != EGame.TMF)
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
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

    private static async Task<Results<Ok<TMFCampaignScoresSnapshotDto>, ValidationProblem, NotFound>> GetLatestGameCampaignSnapshot(
        EGame gameId, 
        string campaignId, 
        IScoresSnapshotService scoresSnapshotService,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (gameId != EGame.TMF)
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
        if (campaignId.Length > 32)
        {
            errors[nameof(campaignId)] = ["The campaign ID length must not exceed 32 characters."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await scoresSnapshotService.GetLatestSnapshotDtoAsync(campaignId, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresRecordDto>>, ValidationProblem>> GetGameCampaignSnapshotRecordsByMapUid(
        EGame gameId,
        string campaignId, 
        DateTimeOffset createdAt, 
        string mapUid, 
        IScoresSnapshotService scoresSnapshotService, 
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (gameId != EGame.TMF)
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
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

        var dtos = await scoresSnapshotService.GetSnapshotRecordDtosAsync(campaignId, createdAt, mapUid, cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFLoginDto>, ValidationProblem, NotFound>> GetGameLogin(
        string gameId, 
        string loginId, 
        ILoginService loginService,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!gameId.Equals(nameof(EGame.TMF), StringComparison.InvariantCultureIgnoreCase))
        {
            errors[nameof(gameId)] = ["Only 'TMF' is supported as game ID."];
        }
        if (loginId.Length > 32)
        {
            errors[nameof(loginId)] = ["The login ID length must not exceed 32 characters."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await loginService.GetTMFDtoAsync(loginId, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}
