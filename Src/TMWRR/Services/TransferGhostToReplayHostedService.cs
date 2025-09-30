using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Entities;

namespace TMWRR.Services;

public sealed class TransferGhostToReplayHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TransferGhostToReplayHostedService> logger;

    public TransferGhostToReplayHostedService(IServiceScopeFactory scopeFactory, ILogger<TransferGhostToReplayHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ghostsToRemove = new List<GhostEntity>();

        foreach (var ghost in db.Ghosts.Include(x => x.Records))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            if (ghost.Data is null or { Length: 0 })
            {
                logger.LogWarning("Ghost {GhostGuid} has no data", ghost.Guid);
                continue;
            }

            var node = Gbx.ParseNode(new MemoryStream(ghost.Data));

            if (node is not CGameCtnReplayRecord replayNode)
            {
                continue;
            }

            ghostsToRemove.Add(ghost);

            var replay = new ReplayEntity
            {
                Data = ghost.Data,
                LastModifiedAt = ghost.LastModifiedAt,
                Etag = ghost.Etag,
                Url = ghost.Url
            };

            foreach (var record in ghost.Records)
            {
                record.Ghost = null;
                record.Replay = replay;
            }

            if (replayNode.Ghosts is null)
            {
                logger.LogWarning("Replay {GhostGuid} has NULL ghosts", replay.Guid);
                continue;
            }

            if (replayNode.Ghosts.Count > 1)
            {
                logger.LogWarning("Replay {GhostGuid} has {GhostCount} ghosts, this is very odd!", replay.Guid, replayNode.Ghosts.Count);
            }

            foreach (var (i, ghostNode) in replayNode.Ghosts.Index())
            {
                var replayGhost = new ReplayGhostEntity
                {
                    Replay = replay,
                    Order = i
                };

                foreach (var (j, cp) in ghostNode.Checkpoints?.Index() ?? [])
                {
                    replayGhost.Checkpoints.Add(new GhostCheckpointEntity
                    {
                        Time = cp.Time,
                        StuntsScore = cp.StuntsScore,
                        Speed = cp.Speed,
                        Order = j
                    });
                }

                replay.Ghosts.Add(replayGhost);
            }
        }

        db.Ghosts.RemoveRange(ghostsToRemove);

        await db.SaveChangesAsync(stoppingToken);
    }
}
