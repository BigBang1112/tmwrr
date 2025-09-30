using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TMWRR.Data;
using TMWRR.Entities;
using TMWRR.Options;

namespace TMWRR.Services;

public sealed class GhostFillDataHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptions<DatabaseOptions> options;
    private readonly ILogger<GhostFillDataHostedService> logger;

    public GhostFillDataHostedService(IServiceScopeFactory scopeFactory, IOptions<DatabaseOptions> options, ILogger<GhostFillDataHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.FillMissingGhostInfo)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unfilledGhosts = await db.Ghosts
            .Where(x => !x.Checkpoints.Any())
            .ToListAsync(stoppingToken);

        foreach (var ghost in unfilledGhosts)
        {
            using var ms = new MemoryStream(ghost.Data);

            try
            {
                var ghostNode = Gbx.ParseNode<CGameCtnGhost>(ms);

                foreach (var (i, cp) in ghostNode.Checkpoints?.Index() ?? [])
                {
                    ghost.Checkpoints.Add(new GhostCheckpointEntity
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
                logger.LogWarning(ex, "Failed to parse ghost {GhostGuid}", ghost.Guid);
                continue;
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}
