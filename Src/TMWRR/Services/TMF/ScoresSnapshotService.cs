using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TMWRR.Data;
using TMWRR.Entities;

namespace TMWRR.Services.TMF;

public interface IScoresSnapshotService
{
    Task<bool> CampaignSnapshotExistsAsync(string campaignId, DateTimeOffset createdAt, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new snapshot of the TMF campaign scores. Expects that the snapshot is populated with records.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveSnapshotAsync(TMFCampaignScoresSnapshot snapshot, CancellationToken cancellationToken);
    ValueTask<IEnumerable<TMFCampaignScoresRecord>> GetLatestRecordsAsync(IEnumerable<Map> maps, CancellationToken cancellationToken);
}

public sealed class ScoresSnapshotService : IScoresSnapshotService
{
    private readonly AppDbContext db;

    public ScoresSnapshotService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<bool> CampaignSnapshotExistsAsync(string campaignId, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        return await db.TMFCampaignScoresSnapshots
            .AnyAsync(x => x.CampaignId == campaignId && x.CreatedAt == createdAt, cancellationToken);
    }

    public async Task SaveSnapshotAsync(TMFCampaignScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

        await db.TMFCampaignScoresSnapshots.AddAsync(snapshot, cancellationToken);
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
}
