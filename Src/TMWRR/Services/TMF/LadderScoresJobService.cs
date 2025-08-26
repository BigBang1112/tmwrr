using ManiaAPI.Xml.TMUF;
using TMWRR.Entities.TMF;

namespace TMWRR.Services.TMF;

public interface ILadderScoresJobService
{
    Task<bool> ProcessAsync(LadderZone ladder, TMFLadderScoresSnapshot snapshot, CancellationToken cancellationToken);
}

public class LadderScoresJobService : ILadderScoresJobService
{
    private readonly IScoresSnapshotService scoresSnapshotService;

    public LadderScoresJobService(IScoresSnapshotService scoresSnapshotService)
    {
        this.scoresSnapshotService = scoresSnapshotService;
    }

    public async Task<bool> ProcessAsync(LadderZone ladder, TMFLadderScoresSnapshot snapshot, CancellationToken cancellationToken)
    {
        snapshot.PlayerCount = ladder.PlayerCount;

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
                return false;
            }
        }

        foreach (var (i, (rank, points)) in rankPoints.Index())
        {
            snapshot.XYs.Add(new TMFLadderScoresXY
            {
                Rank = rank,
                Points = points,
                Order = i,
                Snapshot = snapshot
            });
        }

        return true;
    }
}
