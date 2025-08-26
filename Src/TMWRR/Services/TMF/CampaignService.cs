using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Dtos.TMF;

namespace TMWRR.Services.TMF;

public interface ICampaignService
{
    Task<IEnumerable<TMFCampaignDto>> GetAllDtosAsync(CancellationToken cancellationToken);
    Task<TMFCampaignDto?> GetDtoAsync(string id, CancellationToken cancellationToken);
    Task<TMFCampaignMapDto?> GetMapDtoAsync(string campaignId, string mapUid, CancellationToken cancellationToken);
    Task<IEnumerable<TMFCampaignMapDto>> GetMapDtosAsync(string campaignId, CancellationToken cancellationToken);
}

public sealed class CampaignService : ICampaignService
{
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly AppDbContext db;

    public CampaignService(IScoresSnapshotService scoresSnapshotService, AppDbContext db)
    {
        this.scoresSnapshotService = scoresSnapshotService;
        this.db = db;
    }

    public async Task<IEnumerable<TMFCampaignDto>> GetAllDtosAsync(CancellationToken cancellationToken)
    {
        return await db.TMFCampaigns
            .Select(x => new TMFCampaignDto
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TMFCampaignDto?> GetDtoAsync(string id, CancellationToken cancellationToken)
    {
        return await db.TMFCampaigns
            .Include(x => x.Maps)
            .Select(x => new TMFCampaignDto
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
                Maps = x.Maps.OrderBy(m => m.Order).Select(m => new MapDto
                {
                    MapUid = m.MapUid,
                    Name = m.Name,
                    DeformattedName = m.DeformattedName
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TMFCampaignMapDto?> GetMapDtoAsync(string campaignId, string mapUid, CancellationToken cancellationToken)
    {
        var map = await db.Maps
            .Where(x => x.TMFCampaignId == campaignId && x.MapUid == mapUid)
            .Select(m => new MapDto
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

        return new TMFCampaignMapDto
        {
            Map = map,
            RecordCount = recordCount
        };
    }

    public async Task<IEnumerable<TMFCampaignMapDto>> GetMapDtosAsync(string campaignId, CancellationToken cancellationToken)
    {
        var maps = await db.Maps
            .Where(x => x.TMFCampaignId == campaignId)
            .OrderBy(x => x.Order)
            .Select(m => new MapDto
            {
                MapUid = m.MapUid,
                Name = m.Name,
                DeformattedName = m.DeformattedName
            })
            .ToListAsync(cancellationToken);

        if (maps.Count == 0)
        {
            return [];
        }

        var recordCount = await scoresSnapshotService.GetLatestPlayerCountsAsync(campaignId, cancellationToken);

        return maps.Select(m => new TMFCampaignMapDto
        {
            Map = m,
            RecordCount = recordCount.TryGetValue(m.MapUid, out var count) ? count : null
        });
    }
}
