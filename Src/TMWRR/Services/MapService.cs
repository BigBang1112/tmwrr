using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Immutable;
using TMWRR.Data;
using TMWRR.Api;
using TMWRR.Api.TMF;
using TMWRR.Entities;
using TMWRR.Extensions;

namespace TMWRR.Services;

public interface IMapService
{
    ValueTask<IDictionary<string, MapEntity>> PopulateAsync(IEnumerable<string> mapUids, CancellationToken cancellationToken);
    Task<MapEntity> GetOrCreateAsync(string mapUid, CancellationToken cancellationToken);
    Task<Map?> GetDtoAsync(string mapUid, CancellationToken cancellationToken);
}

public sealed class MapService : IMapService
{
    private readonly AppDbContext db;
    private readonly HybridCache cache;
    private readonly ILogger<MapService> logger;

    public MapService(AppDbContext db, HybridCache cache, ILogger<MapService> logger)
    {
        this.db = db;
        this.cache = cache;
        this.logger = logger;
    }

    public async ValueTask<IDictionary<string, MapEntity>> PopulateAsync(IEnumerable<string> mapUids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapUids);

        if (!mapUids.Any())
        {
            logger.LogWarning("No map UIDs to populate with new data.");
            return new Dictionary<string, MapEntity>();
        }

        // MapUid is not an unique index, so the duplicates need to be handled manually.
        logger.LogInformation("Gathering {Count} unique map UIDs...", mapUids.Distinct().Count());

        var maps = await db.Maps
            .Include(x => x.TMFCampaign) // needed for the CampaignScoresJobService
            .Where(e => mapUids.Contains(e.MapUid))
            .ToDictionaryAsync(x => x.MapUid, cancellationToken);

        logger.LogInformation("Found {Count} existing maps in database, will add {MissingCount} new ones...", maps.Count, mapUids.Distinct().Count() - maps.Count);

        var missingMaps = mapUids.Except(maps.Keys).Select(x => new MapEntity
        {
            MapUid = x
        }).ToList();

        if (missingMaps.Count == 0)
        {
            return maps;
        }

        logger.LogInformation("Adding {Count} new maps to database...", missingMaps.Count);

        await db.Maps.AddRangeAsync(missingMaps, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var map in missingMaps)
        {
            maps[map.MapUid] = map;
        }

        logger.LogInformation("Returning {Count} maps...", maps.Count);

        return maps;
    }

    public async Task<MapEntity> GetOrCreateAsync(string mapUid, CancellationToken cancellationToken)
    {
        var map = await db.Maps.FirstOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);

        if (map is null)
        {
            map = new MapEntity
            {
                MapUid = mapUid
            };

            await db.Maps.AddAsync(map, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return map;
    }

    public async Task<Map?> GetDtoAsync(string mapUid, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"map-{mapUid}", async token =>
        {
            return await db.Maps
                .Select(x => new Map
                {
                    MapUid = x.MapUid,
                    Name = x.Name,
                    DeformattedName = x.DeformattedName,
                    Author = x.Author == null ? null : new User
                    {
                        Guid = x.Author.Guid,
                        LoginTMF = x.Author.LoginTMF == null ? null : new TMFLogin
                        {
                            Id = x.Author.LoginTMF.Id,
                            Nickname = x.Author.LoginTMF.Nickname,
                            NicknameDeformatted = x.Author.LoginTMF.NicknameDeformatted
                        }
                    },
                    Environment = x.Environment == null ? null : new TMEnvironment
                    {
                        Id = x.Environment.Id,
                        Name = x.Environment.Name ?? x.Environment.Id,
                        Game = x.Environment.Game == null ? null : new Game
                        {
                            Id = x.Environment.Game.Id
                        }
                    },
                    Mode = x.Mode == null ? null : new Mode
                    {
                        Id = x.Mode.Id
                    },
                    AuthorTime = x.AuthorTime,
                    AuthorScore = x.AuthorScore,
                    NbLaps = x.NbLaps,
                    CampaignTMF = x.TMFCampaign == null ? null : new TMFCampaign
                    {
                        Id = x.TMFCampaign.Id,
                        Name = x.TMFCampaign.Name ?? x.TMFCampaign.Name
                    },
                    RecordCountTMF = x.TMFPlayerCounts
                        .OrderByDescending(x => x.Snapshot.CreatedAt)
                        .Select(x => x.Count)
                        .FirstOrDefault(),
                    RecordsTMF = x.TMFRecords
                        .OrderByDescending(x => x.Snapshot.CreatedAt)
                        .Select(x => new TMFCampaignScoresRecord
                        {
                            Rank = x.Rank,
                            Score = x.Score,
                            Player = new TMFLogin
                            {
                                Id = x.Player.Id,
                                Nickname = x.Player.Nickname,
                                NicknameDeformatted = x.Player.NicknameDeformatted
                            },
                            Ghost = x.Ghost == null ? null : new Ghost
                            {
                                Guid = x.Ghost.Guid,
                                Timestamp = x.Ghost.LastModifiedAt
                            },
                            Replay = x.Replay == null ? null : new Replay
                            {
                                Guid = x.Replay.Guid,
                                Timestamp = x.Replay.LastModifiedAt,
                            }
                        }).ToNullableImmutableListIfEmpty(),
                    Order = x.Order,
                    FileName = x.FileName
                })
                .FirstOrDefaultAsync(x => x.MapUid == mapUid, token);
        }, new() { Expiration = TimeSpan.FromHours(1) }, ["map"], cancellationToken);
    }
}
