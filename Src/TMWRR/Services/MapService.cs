using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Entities;

namespace TMWRR.Services;

public interface IMapService
{
    ValueTask<IDictionary<string, Map>> PopulateAsync(IEnumerable<string> mapUids, CancellationToken cancellationToken);
    Task<Map> GetOrCreateAsync(string mapUid, CancellationToken cancellationToken);
    Task<MapDto?> GetDtoAsync(string mapUid, CancellationToken cancellationToken);
}

public sealed class MapService : IMapService
{
    private readonly AppDbContext db;

    public MapService(AppDbContext db)
    {
        this.db = db;
    }

    public async ValueTask<IDictionary<string, Map>> PopulateAsync(IEnumerable<string> mapUids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapUids, nameof(mapUids));

        if (!mapUids.Any())
        {
            return new Dictionary<string, Map>();
        }

        // MapUid is not an unique index, so the duplicates need to be handled manually.

        var maps = await db.Maps
            .Where(e => mapUids.Contains(e.MapUid))
            .ToDictionaryAsync(x => x.MapUid, cancellationToken);

        var missingMaps = mapUids.Except(maps.Keys).Select(x => new Map
        {
            MapUid = x
        }).ToList();

        if (missingMaps.Count == 0)
        {
            return maps;
        }

        await db.Maps.AddRangeAsync(missingMaps, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var map in missingMaps)
        {
            maps[map.MapUid] = map;
        }

        return maps;
    }

    public async Task<Map> GetOrCreateAsync(string mapUid, CancellationToken cancellationToken)
    {
        var map = await db.Maps.FirstOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);

        if (map is null)
        {
            map = new Map
            {
                MapUid = mapUid
            };

            await db.Maps.AddAsync(map, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return map;
    }

    public async Task<MapDto?> GetDtoAsync(string mapUid, CancellationToken cancellationToken)
    {
        return await db.Maps
            .Select(x => new MapDto
            {
                MapUid = x.MapUid,
                Name = x.Name,
                DeformattedName = x.DeformattedName,
                Author = x.Author == null ? null : new UserDto
                {
                    Guid = x.Author.Guid,
                    LoginTMF = x.Author.LoginTMF == null ? null : new TMFLoginDto
                    {
                        Id = x.Author.LoginTMF.Id,
                        Nickname = x.Author.LoginTMF.Nickname
                    }
                },
                Environment = x.Environment == null ? null : new TMEnvironmentDto
                {
                    Id = x.Environment.Id,
                    Name = x.Environment.Name ?? x.Environment.Id,
                    Game = x.Environment.Game == null ? null : new GameDto
                    {
                        Id = x.Environment.Game.Id
                    }
                },
                Mode = x.Mode == null ? null : new ModeDto
                {
                    Id = x.Mode.Id
                },
                AuthorTime = x.AuthorTime,
                AuthorScore = x.AuthorScore,
                NbLaps = x.NbLaps,
                CampaignTMF = x.TMFCampaign == null ? null : new TMFCampaignDto
                {
                    Id = x.TMFCampaign.Id,
                    Name = x.TMFCampaign.Name ?? x.TMFCampaign.Name
                },
                Order = x.Order,
                FileName = x.FileName
            })
            .FirstOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);
    }
}
