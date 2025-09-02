using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TmEssentials;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Entities;
using TMWRR.Entities.TMF;

namespace TMWRR.Services;

public interface IReplayService
{
    Task<Replay?> CreateReplayAsync(Map map, TMFLogin login, int expectedScore, CancellationToken cancellationToken);
    Task<DownloadContentDto?> GetReplayDownloadAsync(Guid guid, CancellationToken cancellationToken);
    Task<ReplayDto?> GetReplayDtoAsync(Guid guid, CancellationToken cancellationToken);
}

public sealed class ReplayService : IReplayService
{
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<GhostService> logger;

    public ReplayService(AppDbContext db, HttpClient http, ILogger<GhostService> logger)
    {
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<Replay?> CreateReplayAsync(Map map, TMFLogin login, int expectedScore, CancellationToken cancellationToken)
    {
        if (map.TMFCampaign is null || login.RegistrationId is null)
        {
            logger.LogWarning("Cannot download replay for map {MapUid} and login {Login}, probably missing registration ID", map.MapUid, login.Id);
            return null;
        }

        logger.LogInformation("Downloading replay for map {MapUid} and login {Login}...", map.MapUid, login.Id);

        var url = $"http://data.trackmaniaforever.com/official_replays/records/{map.TMFCampaign.Section}/{map.TMFCampaign.StartId + map.Order}/{login.RegistrationId}.replay.gbx";

        using var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to download replay for map {MapUid} and login {Login}, status code {StatusCode}", map.MapUid, login.Id, response.StatusCode);
            return null;
        }

        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var replay = new Replay
        {
            Data = data,
            LastModifiedAt = response.Content.Headers.LastModified,
            Etag = response.Headers.ETag?.Tag,
            Url = url
        };

        await using var ms = new MemoryStream(data);

        try
        {
            var replayNode = Gbx.ParseNode<CGameCtnReplayRecord>(ms);

            if (replayNode.Ghosts is null)
            {
                logger.LogWarning("Downloaded replay has NULL ghosts for map {MapUid} and login {Login}", map.MapUid, login.Id);
                return replay;
            }

            if (replayNode.Ghosts.Count > 1)
            {
                logger.LogWarning("Downloaded replay has {GhostCount} ghosts for map {MapUid} and login {Login}, this is very odd!", replayNode.Ghosts.Count, map.MapUid, login.Id);
            }

            foreach (var (i, ghostNode) in replayNode.Ghosts.Index())
            {
                if (i == 0)
                {
                    if (map.IsStunts())
                    {
                        if (ghostNode.StuntScore != expectedScore)
                        {
                            logger.LogWarning("Downloaded replay (first ghost) stunt score {GhostScore} does not match expected score {ExpectedScore} for map {MapUid} and login {Login}", ghostNode.StuntScore, expectedScore, map.MapUid, login.Id);
                            return replay;
                        }
                    }
                    else if (map.IsPlatform())
                    {
                        if (ghostNode.Respawns != expectedScore)
                        {
                            logger.LogWarning("Downloaded replay (first ghost) platform score {GhostScore} does not match expected score {ExpectedScore} for map {MapUid} and login {Login}", ghostNode.Respawns, expectedScore, map.MapUid, login.Id);
                            return replay;
                        }
                    }
                    else if (ghostNode.RaceTime != new TimeInt32(expectedScore))
                    {
                        logger.LogWarning("Downloaded replay (first ghost) time {GhostTime} does not match expected time {ExpectedTime} for map {MapUid} and login {Login}", ghostNode.RaceTime, expectedScore, map.MapUid, login.Id);
                        return replay;
                    }
                }

                var ghost = new ReplayGhost
                {
                    Replay = replay,
                    Order = i
                };

                foreach (var (j, cp) in ghostNode.Checkpoints?.Index() ?? [])
                {
                    ghost.Checkpoints.Add(new GhostCheckpoint
                    {
                        Time = cp.Time,
                        StuntsScore = cp.StuntsScore,
                        Speed = cp.Speed,
                        Order = j
                    });
                }

                replay.Ghosts.Add(ghost);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse ghost for map {MapUid} and login {Login}", map.MapUid, login.Id);
        }

        return replay;
    }

    public async Task<DownloadContentDto?> GetReplayDownloadAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.Replays
            .Where(x => x.Guid == guid)
            .Select(x => new DownloadContentDto
            {
                Data = x.Data,
                LastModifiedAt = x.LastModifiedAt,
                Etag = x.Etag
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReplayDto?> GetReplayDtoAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.Replays
            .Where(x => x.Guid == guid)
            .Select(x => new ReplayDto
            {
                Guid = x.Guid,
                Timestamp = x.LastModifiedAt,
                Url = x.Url,
                Ghosts = x.Ghosts.OrderBy(g => g.Order).Select(g => new ReplayGhostDto
                {
                    Checkpoints = g.Checkpoints.OrderBy(c => c.Order).Select(c => new GhostCheckpointDto
                    {
                        Time = c.Time,
                        StuntsScore = c.StuntsScore,
                        Speed = c.Speed
                    }).ToImmutableList()
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
