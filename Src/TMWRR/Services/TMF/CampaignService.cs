using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TMWRR.Data;
using TMWRR.Dtos;

namespace TMWRR.Services.TMF;

public interface ICampaignService
{
    Task<IEnumerable<TMFCampaignDto>> GetAllDtosAsync(CancellationToken cancellationToken);
    Task<TMFCampaignDto?> GetDtoAsync(string id, CancellationToken cancellationToken);
}

public sealed class CampaignService : ICampaignService
{
    private readonly AppDbContext db;

    public CampaignService(AppDbContext db)
    {
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
                    Name = m.Name
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
