using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Dtos.TMF;
using TMWRR.Entities;
using TMWRR.Entities.TMF;

namespace TMWRR.Services.TMF;

public interface IScoresSnapshotService
{
    Task<bool> CampaignSnapshotExistsAsync(string campaignId, DateTimeOffset createdAt, CancellationToken cancellationToken);
    Task<bool> LadderSnapshotExistsAsync(DateTimeOffset createdAt, CancellationToken cancellationToken);
    Task<bool> GeneralSnapshotExistsAsync(DateTimeOffset createdAt, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new snapshot of the TMF campaign scores. Expects that the snapshot is populated with records.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveSnapshotAsync(TMFCampaignScoresSnapshot snapshot, CancellationToken cancellationToken);
    /// <summary>
    /// Saves a new snapshot of the TMF ladder scores. Expects that the snapshot is populated with a graph.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveSnapshotAsync(TMFLadderScoresSnapshot snapshot, CancellationToken cancellationToken);
    /// <summary>
    /// Saves a new snapshot of the TMF campaign scores. Expects that the snapshot is populated with records.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveSnapshotAsync(TMFGeneralScoresSnapshot snapshot, CancellationToken cancellationToken);
    ValueTask<IEnumerable<TMFCampaignScoresRecord>> GetLatestRecordsAsync(IEnumerable<Map> maps, CancellationToken cancellationToken);
    Task<TMFCampaignScoresSnapshotDto?> GetLatestSnapshotDtoAsync(string campaignId, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignScoresRecordDto>> GetLatestRecordDtosAsync(string campaignId, string mapUid, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignScoresRecordDto>> GetSnapshotRecordDtosAsync(string campaignId, DateTimeOffset createdAt, string mapUid, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignScoresSnapshotDto>> GetMapSnapshotDtosAsync(string mapUid, CancellationToken cancellationToken);
    Task<TMFCampaignScoresRecord?> GetRecordAsync(Map map, TMFLogin login, int score, CancellationToken cancellationToken);
    ValueTask<IDictionary<string, int>> GetLatestPlayerCountsAsync(IEnumerable<Map> values, CancellationToken cancellationToken);
    ValueTask<IDictionary<string, int>> GetLatestPlayerCountsAsync(string campaignId, CancellationToken cancellationToken);
    Task<int?> GetLatestPlayerCountAsync(string mapUid, CancellationToken cancellationToken);
    Task<TMFLadderScoresSnapshot?> GetLatestLadderSnapshotAsync(CancellationToken cancellationToken);
    Task<TMFGeneralScoresSnapshot?> GetLatestGeneralSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class ScoresSnapshotService : IScoresSnapshotService
{
    private readonly AppDbContext db;
    private readonly HybridCache hybridCache;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<ScoresSnapshotService> logger;

    public ScoresSnapshotService(
        AppDbContext db, 
        HybridCache hybridCache, 
        IOutputCacheStore outputCache,
        ILogger<ScoresSnapshotService> logger)
    {
        this.db = db;
        this.hybridCache = hybridCache;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    public async Task<bool> CampaignSnapshotExistsAsync(string campaignId, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking if TMF campaign snapshot exists for campaign {CampaignId} at {CreatedAt}", campaignId, createdAt);
        return await db.TMFCampaignScoresSnapshots
            .AnyAsync(x => x.CampaignId == campaignId && x.CreatedAt == createdAt, cancellationToken);
    }

    public async Task<bool> LadderSnapshotExistsAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking if TMF ladder snapshot exists at {CreatedAt}", createdAt);
        return await db.TMFLadderScoresSnapshots
            .AnyAsync(x => x.CreatedAt == createdAt, cancellationToken);
    }

    public async Task<bool> GeneralSnapshotExistsAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking if TMF general snapshot exists at {CreatedAt}", createdAt);
        return await db.TMFGeneralScoresSnapshots
            .AnyAsync(x => x.CreatedAt == createdAt, cancellationToken);
    }

    public async Task SaveSnapshotAsync(TMFCampaignScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await db.TMFCampaignScoresSnapshots.AddAsync(snapshot, cancellationToken);

        logger.LogInformation("Saving TMF campaign snapshot for campaign {CampaignId} at {CreatedAt} with {RecordCount} records and {PlayerCount} player counts",
            snapshot.CampaignId, snapshot.CreatedAt, snapshot.Records.Count, snapshot.PlayerCounts.Count);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Evicting TMF campaign snapshot cache for campaign {CampaignId}...", snapshot.CampaignId);

        await outputCache.EvictByTagAsync("snapshot-campaign-tmf", CancellationToken.None);
        await hybridCache.RemoveByTagAsync("snapshot-campaign-tmf", CancellationToken.None);

        // uncache map DTOs where recordCountTMF is stored
        foreach (var mapUid in snapshot.PlayerCounts.Select(x => x.Map.MapUid))
        {
            await hybridCache.RemoveAsync($"map-{mapUid}", CancellationToken.None);
        }

        // same might be needed for recordsTMF
        logger.LogInformation("TMF campaign snapshot saved.");
    }

    public async Task SaveSnapshotAsync(TMFLadderScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await db.TMFLadderScoresSnapshots.AddAsync(snapshot, cancellationToken);

        logger.LogInformation("Saving TMF ladder snapshot at {CreatedAt} with {XYCount} XY points...",
            snapshot.CreatedAt, snapshot.XYs.Count);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("TMF ladder snapshot saved.");
    }

    public async Task SaveSnapshotAsync(TMFGeneralScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await db.TMFGeneralScoresSnapshots.AddAsync(snapshot, cancellationToken);

        logger.LogInformation("Saving TMF general snapshot at {CreatedAt} with {PlayerCount} players...",
            snapshot.CreatedAt, snapshot.Players.Count);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("TMF general snapshot saved.");
    }

    public async ValueTask<IEnumerable<TMFCampaignScoresRecord>> GetLatestRecordsAsync(IEnumerable<Map> maps, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(maps);

        if (!maps.Any())
        {
            return [];
        }

        var records = await db.TMFCampaignScoresRecords
            .Where(x => maps.Contains(x.Map))
            .GroupBy(x => new { x.Map.MapUid, x.Order })
            .Select(g => g.OrderByDescending(x => x.Snapshot.CreatedAt).First())
            .ToListAsync(cancellationToken);

        return records;
    }

    public async Task<TMFCampaignScoresSnapshotDto?> GetLatestSnapshotDtoAsync(string campaignId, CancellationToken cancellationToken)
    {
        return await hybridCache.GetOrCreateAsync($"snapshot-tmf-latest-{campaignId}", async token =>
        {
            return await db.TMFCampaignScoresSnapshots
                .Select(x => new TMFCampaignScoresSnapshotDto
                {
                    Campaign = new TMFCampaignDto
                    {
                        Id = x.Campaign.Id,
                        Name = x.Campaign.Name
                    },
                    CreatedAt = x.CreatedAt,
                    PublishedAt = x.PublishedAt,
                    NoChanges = x.NoChanges
                })
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.Campaign.Id == campaignId, token);
        }, new() { Expiration = TimeSpan.FromDays(1) }, ["snapshot-campaign-tmf"], cancellationToken);
    }

    public async Task<IEnumerable<TMFCampaignScoresRecordDto>> GetLatestRecordDtosAsync(string campaignId, string mapUid, CancellationToken cancellationToken)
    {
        return await hybridCache.GetOrCreateAsync($"snapshot-tmf-records-latest-{campaignId}-{mapUid}", async token =>
        {
            var records = await db.TMFCampaignScoresRecords
                .Include(x => x.Player)
                .Include(x => x.Ghost)
                .Include(x => x.Replay)
                .Where(x => x.Snapshot.Campaign.Id == campaignId && x.Map.MapUid == mapUid)
                .GroupBy(x => x.Order)
                .Select(g => g.OrderByDescending(x => x.Snapshot.CreatedAt).First())
                .AsNoTracking()
                .ToListAsync(token);

            var playerCount = await GetLatestPlayerCountAsync(mapUid, token);
            var skillpointRanks = playerCount > 0
                ? SkillpointCalculator.GetRanksForSkillpoints(records.Select(x => x.Rank).ToArray())
                : [];

            return records.OrderBy(x => x.Order).Select((x, i) => new TMFCampaignScoresRecordDto
            {
                Rank = x.Rank,
                Score = x.Score,
                Player = new TMFLoginDto
                {
                    Id = x.Player.Id,
                    Nickname = x.Player.Nickname,
                    NicknameDeformatted = x.Player.NicknameDeformatted
                },
                Skillpoints = playerCount > 0 ? SkillpointCalculator.CalculateSkillpoints(playerCount.Value, skillpointRanks[i]) : null,
                Ghost = x.Ghost is null ? null : new GhostDto
                {
                    Guid = x.Ghost.Guid,
                    Timestamp = x.Ghost.LastModifiedAt
                },
                Replay = x.Replay is null ? null : new ReplayDto
                {
                    Guid = x.Replay.Guid,
                    Timestamp = x.Replay.LastModifiedAt,
                },
            });
        }, new() { Expiration = TimeSpan.FromDays(1) }, ["snapshot-campaign-tmf"], cancellationToken);
    }

    public async Task<IEnumerable<TMFCampaignScoresRecordDto>> GetSnapshotRecordDtosAsync(string campaignId, DateTimeOffset createdAt, string mapUid, CancellationToken cancellationToken)
    {
        return await hybridCache.GetOrCreateAsync($"snapshot-tmf-latest-{campaignId}-{createdAt}-{mapUid}", async token =>
        {
            var records = await db.TMFCampaignScoresRecords
                .Include(x => x.Player)
                .Include(x => x.Ghost)
                .Where(x => x.Snapshot.Campaign.Id == campaignId && x.Snapshot.CreatedAt == createdAt && x.Map.MapUid == mapUid)
                .OrderBy(x => x.Order)
                .Select(x => new TMFCampaignScoresRecordDto
                {
                    Rank = x.Rank,
                    Score = x.Score,
                    Player = new TMFLoginDto
                    {
                        Id = x.Player.Id,
                        Nickname = x.Player.Nickname,
                        NicknameDeformatted = x.Player.NicknameDeformatted
                    },
                    Ghost = x.Ghost == null ? null : new GhostDto
                    {
                        Guid = x.Ghost.Guid,
                        Timestamp = x.Ghost.LastModifiedAt
                    },
                    Replay = x.Replay == null ? null : new ReplayDto
                    {
                        Guid = x.Replay.Guid,
                        Timestamp = x.Replay.LastModifiedAt,
                    }
                })
                .ToListAsync(token);

            var playerCount = await GetPlayerCountAsync(mapUid, createdAt, token);
            var skillpointRanks = playerCount > 0
                ? SkillpointCalculator.GetRanksForSkillpoints(records.Select(x => x.Rank).ToArray())
                : [];

            return records.Select((x, i) =>
            {
                x.Skillpoints = playerCount > 0 ? SkillpointCalculator.CalculateSkillpoints(playerCount.Value, skillpointRanks[i]) : null;
                return x;
            });
        }, new() { Expiration = TimeSpan.FromMinutes(10) }, ["snapshot-campaign-tmf"], cancellationToken);
    }

    public async Task<IEnumerable<TMFCampaignScoresSnapshotDto>> GetMapSnapshotDtosAsync(string mapUid, CancellationToken cancellationToken)
    {
        var snapshots = await db.TMFCampaignScoresRecords
            .Include(x => x.Snapshot.Campaign)
            .Where(x => x.Map.MapUid == mapUid && !x.Snapshot.NoChanges)
            .Select(x => x.Snapshot)
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return snapshots.Select(x => new TMFCampaignScoresSnapshotDto
        {
            Campaign = new TMFCampaignDto
            {
                Id = x.Campaign.Id,
                Name = x.Campaign.Name
            },
            CreatedAt = x.CreatedAt,
            PublishedAt = x.PublishedAt,
            NoChanges = x.NoChanges
        });
    }

    public async Task<TMFCampaignScoresRecord?> GetRecordAsync(Map map, TMFLogin login, int score, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresRecords
            .Include(x => x.Ghost)
            .FirstOrDefaultAsync(x => x.Map == map && x.Player == login && x.Score == score, cancellationToken);
    }

    public async ValueTask<IDictionary<string, int>> GetLatestPlayerCountsAsync(IEnumerable<Map> values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!values.Any())
        {
            return new Dictionary<string, int>();
        }

        return await db.TMFCampaignScoresPlayerCounts
            .Where(x => values.Contains(x.Map))
            .GroupBy(x => x.Map.MapUid)
            .Select(g => g.OrderByDescending(x => x.Snapshot.CreatedAt).First())
            .ToDictionaryAsync(x => x.Map.MapUid, x => x.Count, cancellationToken);
    }

    public async ValueTask<IDictionary<string, int>> GetLatestPlayerCountsAsync(string campaignId, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresPlayerCounts
            .Include(x => x.Map)
            .Where(x => x.Map.TMFCampaignId == campaignId)
            .GroupBy(x => x.Map.MapUid)
            .Select(g => g.OrderByDescending(x => x.Snapshot.CreatedAt).First())
            .ToDictionaryAsync(x => x.Map.MapUid, x => x.Count, cancellationToken);
    }

    public async Task<int?> GetLatestPlayerCountAsync(string mapUid, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresPlayerCounts
            .Include(x => x.Map)
            .Where(x => x.Map.MapUid == mapUid)
            .OrderByDescending(x => x.Snapshot.CreatedAt)
            .Select(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetPlayerCountAsync(string mapUid, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresPlayerCounts
            .Include(x => x.Map)
            .Where(x => x.Map.MapUid == mapUid && x.Snapshot.CreatedAt == createdAt)
            .Select(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TMFLadderScoresSnapshot?> GetLatestLadderSnapshotAsync(CancellationToken cancellationToken)
    {
        return await db.TMFLadderScoresSnapshots
            .Include(x => x.XYs)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TMFGeneralScoresSnapshot?> GetLatestGeneralSnapshotAsync(CancellationToken cancellationToken)
    {
        return await db.TMFGeneralScoresSnapshots
            .Include(x => x.Players)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
