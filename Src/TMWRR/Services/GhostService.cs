using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TmEssentials;
using TMWRR.Data;
using TMWRR.Api;
using TMWRR.Entities;
using TMWRR.Entities.TMF;

namespace TMWRR.Services;

public interface IGhostService
{
    Task<GhostEntity?> CreateGhostAsync(MapEntity map, TMFLoginEntity login, int expectedScore, CancellationToken cancellationToken);
    Task<DownloadContent?> GetGhostDownloadAsync(Guid guid, CancellationToken cancellationToken);
    Task<IEnumerable<GhostCheckpoint>> GetGhostCheckpointDtosAsync(Guid guid, CancellationToken cancellationToken);
    Task<Ghost?> GetGhostDtoAsync(Guid guid, CancellationToken cancellationToken);
}

public sealed class GhostService : IGhostService
{
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<GhostService> logger;

    public GhostService(AppDbContext db, HttpClient http, ILogger<GhostService> logger)
    {
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<GhostEntity?> CreateGhostAsync(MapEntity map, TMFLoginEntity login, int expectedScore, CancellationToken cancellationToken)
    {
        if (map.TMFCampaign is null || login.RegistrationId is null)
        {
            logger.LogWarning("Cannot download ghost for map {MapUid} and login {Login}, probably missing registration ID", map.MapUid, login.Id);
            return null;
        }

        logger.LogInformation("Downloading ghost for map {MapUid} and login {Login}...", map.MapUid, login.Id);

        var url = $"http://data.trackmaniaforever.com/official_replays/records/{map.TMFCampaign.Section}/{map.TMFCampaign.StartId + map.Order}/{login.RegistrationId}.replay.gbx";

        using var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to download ghost for map {MapUid} and login {Login}, status code {StatusCode}", map.MapUid, login.Id, response.StatusCode);
            return null;
        }

        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var checkpoints = new List<GhostCheckpointEntity>();

        await using var ms = new MemoryStream(data);

        try
        {
            var ghostNode = Gbx.ParseNode<CGameCtnGhost>(ms);

            if (map.IsStunts())
            {
                if (ghostNode.StuntScore != expectedScore)
                {
                    logger.LogWarning("Downloaded ghost stunt score {GhostScore} does not match expected score {ExpectedScore} for map {MapUid} and login {Login}", ghostNode.StuntScore, expectedScore, map.MapUid, login.Id);
                    return null;
                }
            }
            else if (map.IsPlatform())
            {
                if (ghostNode.Respawns != expectedScore)
                {
                    logger.LogWarning("Downloaded ghost platform score {GhostScore} does not match expected score {ExpectedScore} for map {MapUid} and login {Login}", ghostNode.Respawns, expectedScore, map.MapUid, login.Id);
                    return null;
                }
            }
            else if (ghostNode.RaceTime != new TimeInt32(expectedScore))
            {
                logger.LogWarning("Downloaded ghost time {GhostTime} does not match expected time {ExpectedTime} for map {MapUid} and login {Login}", ghostNode.RaceTime, expectedScore, map.MapUid, login.Id);
                return null;
            }

            foreach (var (i, cp) in ghostNode.Checkpoints?.Index() ?? [])
            {
                checkpoints.Add(new GhostCheckpointEntity
                {
                    Time = cp.Time,
                    StuntsScore = cp.StuntsScore,
                    Speed = cp.Speed,
                    Order = i
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse ghost for map {MapUid} and login {Login}", map.MapUid, login.Id);
        }

        return new GhostEntity
        {
            Data = data,
            LastModifiedAt = response.Content.Headers.LastModified,
            Etag = response.Headers.ETag?.Tag,
            Url = url,
            Checkpoints = checkpoints
        };
    }

    public async Task<DownloadContent?> GetGhostDownloadAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.Ghosts
            .Where(x => x.Guid == guid)
            .Select(x => new DownloadContent
            {
                Data = x.Data,
                LastModifiedAt = x.LastModifiedAt,
                Etag = x.Etag
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<GhostCheckpoint>> GetGhostCheckpointDtosAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.GhostCheckpoints
            .Where(x => x.Ghost!.Guid == guid)
            .OrderBy(x => x.Order)
            .Select(x => new GhostCheckpoint
            {
                Time = x.Time,
                Speed = x.Speed,
                StuntsScore = x.StuntsScore
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Ghost?> GetGhostDtoAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.Ghosts
            .Where(x => x.Guid == guid)
            .Select(x => new Ghost
            {
                Guid = x.Guid,
                Timestamp = x.LastModifiedAt,
                Url = x.Url,
                Checkpoints = x.Checkpoints.OrderBy(m => m.Order).Select(c => new GhostCheckpoint
                {
                    Time = c.Time,
                    StuntsScore = c.StuntsScore,
                    Speed = c.Speed
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
