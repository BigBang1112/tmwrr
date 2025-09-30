using ManiaAPI.Xml.TMUF;
using TMWRR.Entities.TMF;

namespace TMWRR.Services.TMF;

public interface ILadderScoresJobService
{
    Task<bool> ProcessAsync(LadderZone ladder, TMFLadderScoresSnapshotEntity snapshot, CancellationToken cancellationToken);
}

public class LadderScoresJobService : ILadderScoresJobService
{
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly ILogger<LadderScoresJobService> logger;

    public LadderScoresJobService(IScoresSnapshotService scoresSnapshotService, ILogger<LadderScoresJobService> logger)
    {
        this.scoresSnapshotService = scoresSnapshotService;
        this.logger = logger;
    }

    public async Task<bool> ProcessAsync(LadderZone ladder, TMFLadderScoresSnapshotEntity snapshot, CancellationToken cancellationToken)
    {
        snapshot.PlayerCount = ladder.PlayerCount;

        logger.LogInformation("Player count: {Count}", snapshot.PlayerCount);

        logger.LogInformation("Fetching previous snapshot...");

        var prevSnapshot = await scoresSnapshotService.GetLatestLadderSnapshotAsync(cancellationToken);

        var rankPoints = ladder.Ranks.Zip(ladder.Points).ToList();

        if (prevSnapshot is not null)
        {
            var prevRankPoints = prevSnapshot.XYs
                .OrderBy(x => x.Order)
                .Select(x => (x.Rank, x.Points));

            if (rankPoints.SequenceEqual(prevRankPoints) && prevSnapshot.PlayerCount == ladder.PlayerCount)
            {
                // no changes
                logger.LogInformation("No changes detected, skipping snapshot data creation...");
                return false;
            }
        }

        logger.LogInformation("Populating snapshot with new data...");

        foreach (var (i, (rank, points)) in rankPoints.Index())
        {
            snapshot.XYs.Add(new TMFLadderScoresXYEntity
            {
                Rank = rank,
                Points = points,
                Order = i,
                Snapshot = snapshot
            });
        }

        logger.LogInformation("Returning snapshot...");

        return true;
    }
}
