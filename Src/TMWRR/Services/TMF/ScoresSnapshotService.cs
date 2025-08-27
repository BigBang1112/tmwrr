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
    ValueTask<IEnumerable<TMFCampaignScoresRecord>> GetLatestRecordsAsync(IEnumerable<Map> maps, CancellationToken cancellationToken);
    Task<TMFCampaignScoresSnapshotDto?> GetLatestSnapshotDtoAsync(string campaignId, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignScoresRecordDto>> GetLatestRecordDtosAsync(string campaignId, string mapUid, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignScoresRecordDto>> GetSnapshotRecordDtosAsync(string campaignId, DateTimeOffset createdAt, string mapUid, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignScoresSnapshotDto>> GetAllSnapshotDtosAsync(string mapUid, CancellationToken cancellationToken);
    Task<TMFCampaignScoresRecord?> GetRecordAsync(Map map, TMFLogin login, int score, CancellationToken cancellationToken);
    ValueTask<IDictionary<string, int>> GetLatestPlayerCountsAsync(IEnumerable<Map> values, CancellationToken cancellationToken);
    ValueTask<IDictionary<string, int>> GetLatestPlayerCountsAsync(string campaignId, CancellationToken cancellationToken);
    Task<int?> GetLatestPlayerCountAsync(string mapUid, CancellationToken cancellationToken);
    Task<TMFLadderScoresSnapshot?> GetLatestLadderSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class ScoresSnapshotService : IScoresSnapshotService
{
    private readonly AppDbContext db;
    private readonly HybridCache cache;

    public ScoresSnapshotService(AppDbContext db, HybridCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<bool> CampaignSnapshotExistsAsync(string campaignId, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresSnapshots
            .AnyAsync(x => x.CampaignId == campaignId && x.CreatedAt == createdAt, cancellationToken);
    }

    public async Task<bool> LadderSnapshotExistsAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        return await db.TMFLadderScoresSnapshots
            .AnyAsync(x => x.CreatedAt == createdAt, cancellationToken);
    }

    public async Task SaveSnapshotAsync(TMFCampaignScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

        await db.TMFCampaignScoresSnapshots.AddAsync(snapshot, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // uncache map DTOs where recordCountTMF is stored
        foreach (var mapUid in snapshot.PlayerCounts.Select(x => x.Map.MapUid))
        {
            await cache.RemoveAsync($"map-{mapUid}", CancellationToken.None);
        }
    }

    public async Task SaveSnapshotAsync(TMFLadderScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

        await db.TMFLadderScoresSnapshots.AddAsync(snapshot, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IEnumerable<TMFCampaignScoresRecord>> GetLatestRecordsAsync(IEnumerable<Map> maps, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(maps, nameof(maps));

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
            .FirstOrDefaultAsync(x => x.Campaign.Id == campaignId, cancellationToken);
    }

    public async Task<IEnumerable<TMFCampaignScoresRecordDto>> GetLatestRecordDtosAsync(string campaignId, string mapUid, CancellationToken cancellationToken)
    {
        var records = await db.TMFCampaignScoresRecords
            .Include(x => x.Player)
            .Include(x => x.Ghost)
            .Where(x => x.Snapshot.Campaign.Id == campaignId && x.Map.MapUid == mapUid)
            .GroupBy(x => x.Order)
            .Select(g => g.OrderByDescending(x => x.Snapshot.CreatedAt).First())
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return records.Select(x => new TMFCampaignScoresRecordDto
        {
            Rank = x.Rank,
            Score = x.Score,
            Player = new TMFLoginDto
            {
                Id = x.Player.Id,
                Nickname = x.Player.Nickname,
                NicknameDeformatted = x.Player.NicknameDeformatted
            },
            Order = x.Order,
            Ghost = x.Ghost is null ? null : new GhostDto
            {
                Guid = x.Ghost.Guid,
                Timestamp = x.Ghost.LastModifiedAt
            }
        });
    }

    public async Task<IEnumerable<TMFCampaignScoresRecordDto>> GetSnapshotRecordDtosAsync(string campaignId, DateTimeOffset createdAt, string mapUid, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresRecords
            .Include(x => x.Player)
            .Include(x => x.Ghost)
            .Where(x => x.Snapshot.Campaign.Id == campaignId && x.Snapshot.CreatedAt == createdAt && x.Map.MapUid == mapUid)
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
                Order = x.Order,
                Ghost = x.Ghost == null ? null : new GhostDto
                {
                    Guid = x.Ghost.Guid,
                    Timestamp = x.Ghost.LastModifiedAt
                }
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TMFCampaignScoresSnapshotDto>> GetAllSnapshotDtosAsync(string mapUid, CancellationToken cancellationToken)
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
        ArgumentNullException.ThrowIfNull(values, nameof(values));

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

    public async Task<TMFLadderScoresSnapshot?> GetLatestLadderSnapshotAsync(CancellationToken cancellationToken)
    {
        return await db.TMFLadderScoresSnapshots
            .Include(x => x.XYs)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
