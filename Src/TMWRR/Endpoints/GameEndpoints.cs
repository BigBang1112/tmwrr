using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Api;
using TMWRR.Api.TMF;
using TMWRR.Extensions;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Endpoints;

public static class GameEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Game");

        group.MapGet("/", GetGames)
            .WithSummary("Games")
            .WithDescription("Retrieve a list of all supported games.")
            .CacheOutput(CachePolicy.Games);

        group.MapGet("/{gameId}", GetGame)
            .WithSummary("Game by ID")
            .WithDescription("Retrieve details of a specific game by its ID.")
            .CacheOutput(CachePolicy.Games);


        group.MapGet("/{gameId}/campaigns", GetGameCampaigns)
            .WithSummary("Campaigns for a game")
            .WithDescription("Retrieve a list of all campaigns for the specified game. Currently, only 'TMF' is supported as game ID.")
            .CacheOutput(CachePolicy.Campaigns);

        group.MapGet("/{gameId}/campaigns/{campaignId}", GetGameCampaign)
            .WithSummary("Campaign by ID")
            .WithDescription("Retrieve details of a specific campaign by its ID for the specified game. Currently, only 'TMF' is supported as game ID.")
            .CacheOutput();

        group.MapGet("/{gameId}/campaigns/{campaignId}/maps", GetGameCampaignMaps)
            .WithSummary("Maps for a campaign")
            .WithDescription("Retrieve a list of all maps for the specified campaign in the specified game. Currently, only 'TMF' is supported as game ID.");

        group.MapGet("/{gameId}/campaigns/{campaignId}/maps/{mapUid}", GetGameCampaignMap)
            .WithSummary("Map by UID")
            .WithDescription("Retrieve details of a specific map by its UID for the specified campaign in the specified game. Currently, only 'TMF' is supported as game ID.");

        //group.MapGet("/{gameId}/campaigns/{campaignId}/maps/{mapUid}/records", GetGameCampaignRecordsByMapUid);

        group.MapGet("/{gameId}/campaigns/{campaignId}/snapshots/latest", GetLatestGameCampaignSnapshot)
            .WithSummary("Latest campaign snapshot")
            .WithDescription("Retrieve the latest scores snapshot for the specified campaign in the specified game. Currently, only 'TMF' is supported as game ID.");

        group.MapGet("/{gameId}/campaigns/{campaignId}/snapshots/latest/{mapUid}", GetLatestGameCampaignSnapshotByMapUid)
            .WithSummary("Latest campaign snapshot by map")
            .WithDescription("Retrieve the latest scores snapshot for a specific map UID in the specified campaign of the specified game. Currently, only 'TMF' is supported as game ID.");

        group.MapGet("/{gameId}/campaigns/{campaignId}/snapshots/{createdAt}/{mapUid}/records", GetGameCampaignSnapshotRecordsByMapUid)
            .WithSummary("Snapshot records by map UID")
            .WithDescription("Retrieve all records for a specific map UID from a specific snapshot of the specified campaign in the specified game. Currently, only 'TMF' is supported as game ID.");

        group.MapGet("/{gameId}/logins/{loginId}", GetGameLogin)
            .WithSummary("Game login by ID")
            .WithDescription("Retrieve details of a specific game login by its ID for the specified game. Currently, only 'TMF' is supported as game ID.");
    }

    private static async Task<Ok<IEnumerable<Game>>> GetGames(
        IGameService gameService,
        HttpResponse response, 
        CancellationToken cancellationToken)
    {
        var dtos = await gameService.GetAllDtosAsync(cancellationToken);

        response.ClientCache();

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<Game>, ValidationProblem, NotFound>> GetGame(
        EGame gameId, 
        IGameService gameService, 
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var dto = await gameService.GetDtoAsync(gameId, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        response.ClientCache();

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaign>>, ValidationProblem>> GetGameCampaigns(
        EGame gameId, 
        ICampaignService campaignService,
        HttpResponse response,
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

        response.ClientCache();

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMFCampaign>, ValidationProblem, NotFound>> GetGameCampaign(
        EGame gameId, 
        string campaignId,
        ICampaignService campaignService,
        HttpResponse response,
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

        response.ClientCache();

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignMap>>, ValidationProblem>> GetGameCampaignMaps(
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

    private static async Task<Results<Ok<TMFCampaignMap>, ValidationProblem, NotFound>> GetGameCampaignMap(
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

    /*private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresRecord>>, ValidationProblem>> GetGameCampaignRecordsByMapUid(
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
    }*/

    private static async Task<Results<Ok<TMFCampaignScoresSnapshot>, ValidationProblem, NotFound>> GetLatestGameCampaignSnapshot(
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

    private static async Task<Results<Ok<TMFCampaignScoresSnapshot>, ValidationProblem, NotFound>> GetLatestGameCampaignSnapshotByMapUid(
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

        var dto = await scoresSnapshotService.GetLatestSnapshotDtoAsync(campaignId, mapUid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<IEnumerable<TMFCampaignScoresRecord>>, ValidationProblem>> GetGameCampaignSnapshotRecordsByMapUid(
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

    private static async Task<Results<Ok<TMFLogin>, ValidationProblem, NotFound>> GetGameLogin(
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
