using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Immutable;
using TMWRR.Data;
using TMWRR.Api;
using TMWRR.Api.TMF;

namespace TMWRR.Services.TMF;

public interface ICampaignService
{
    Task<IEnumerable<TMFCampaign>> GetAllDtosAsync(CancellationToken cancellationToken);
    Task<TMFCampaign?> GetDtoAsync(string id, CancellationToken cancellationToken);
    Task<TMFCampaignMap?> GetMapDtoAsync(string campaignId, string mapUid, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignMap>> GetMapDtosAsync(string campaignId, CancellationToken cancellationToken);
}

public sealed class CampaignService : ICampaignService
{
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly AppDbContext db;
    private readonly HybridCache cache;

    public CampaignService(IScoresSnapshotService scoresSnapshotService, AppDbContext db, HybridCache cache)
    {
        this.scoresSnapshotService = scoresSnapshotService;
        this.db = db;
        this.cache = cache;
    }

    public async Task<IEnumerable<TMFCampaign>> GetAllDtosAsync(CancellationToken cancellationToken)
    {
        return await db.TMFCampaigns
            .Select(x => new TMFCampaign
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TMFCampaign?> GetDtoAsync(string id, CancellationToken cancellationToken)
    {
        return await db.TMFCampaigns
            .Include(x => x.Maps)
            .Select(x => new TMFCampaign
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
                Maps = x.Maps.OrderBy(m => m.Order).Select(m => new Map
                {
                    MapUid = m.MapUid,
                    Name = m.Name,
                    DeformattedName = m.DeformattedName
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TMFCampaignMap?> GetMapDtoAsync(string campaignId, string mapUid, CancellationToken cancellationToken)
    {
        var map = await db.Maps
            .Where(x => x.TMFCampaignId == campaignId && x.MapUid == mapUid)
            .Select(m => new Map
            {
                MapUid = m.MapUid,
                Name = m.Name,
                DeformattedName = m.DeformattedName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (map is null)
        {
            return null;
        }

        var recordCount = await scoresSnapshotService.GetLatestPlayerCountAsync(mapUid, cancellationToken);

        return new TMFCampaignMap
        {
            Map = map,
            RecordCount = recordCount
        };
    }

    public async Task<IEnumerable<TMFCampaignMap>> GetMapDtosAsync(string campaignId, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"campaign-maps-{campaignId}", async token =>
        {
            var maps = await db.Maps
                .Where(x => x.TMFCampaignId == campaignId)
                .OrderBy(x => x.Order)
                .Select(m => new Map
                {
                    MapUid = m.MapUid,
                    Name = m.Name,
                    DeformattedName = m.DeformattedName
                })
                .ToListAsync(token);

            if (maps.Count == 0)
            {
                return [];
            }

            var recordCount = await scoresSnapshotService.GetLatestPlayerCountsAsync(campaignId, token);

            return maps.Select(m => new TMFCampaignMap
            {
                Map = m,
                RecordCount = recordCount.TryGetValue(m.MapUid, out var count) ? count : null
            });
        }, new() { Expiration = TimeSpan.FromDays(1) }, ["snapshot-campaign-tmf", "map"], cancellationToken);
    }
}
