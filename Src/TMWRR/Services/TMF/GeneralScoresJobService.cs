using ManiaAPI.Xml.TMUF;
using TMWRR.Entities.TMF;
using TMWRR.Models;

namespace TMWRR.Services.TMF;

public interface IGeneralScoresJobService
{
    Task<TMFGeneralScoreDiff?> ProcessAsync(Leaderboard leaderboard, TMFGeneralScoresSnapshot snapshot, CancellationToken cancellationToken);
}

public class GeneralScoresJobService : IGeneralScoresJobService
{
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly ILoginService loginService;

    public GeneralScoresJobService(IScoresSnapshotService scoresSnapshotService, ILoginService loginService)
    {
        this.scoresSnapshotService = scoresSnapshotService;
        this.loginService = loginService;
    }

    public async Task<TMFGeneralScoreDiff?> ProcessAsync(
        Leaderboard leaderboard, 
        TMFGeneralScoresSnapshot snapshot, 
        CancellationToken cancellationToken)
    {
        snapshot.PlayerCount = leaderboard.Skillpoints.Sum(x => x.Count);

        var nicknamesByLogin = leaderboard.HighScores
            .DistinctBy(x => x.Login)
            .ToDictionary(x => x.Login, x => x.Nickname);

        var playersByLogin = await loginService.PopulateAsync(nicknamesByLogin, cancellationToken);

        var prevSnapshot = await scoresSnapshotService.GetLatestGeneralSnapshotAsync(cancellationToken);

        if (prevSnapshot is null)
        {
            PopulateSnapshot(snapshot, playersByLogin, leaderboard, diff: null);
            return null;
        }

        var oldByLogin = prevSnapshot.Players
            .ToDictionary(r => r.PlayerId, r => new TMFGeneralScore(r.Rank, r.Score, r.PlayerId));
        var newByLogin = leaderboard.HighScores.ToDictionary(r => r.Login, r => new TMFGeneralScore(r.Rank, r.Score, r.Login));

        var diff = TMFGeneralScoreDiff.Calculate(leaderboard, oldByLogin, newByLogin);

        if (diff.IsEmpty)
        {
            // No changes, skip snapshot creation
            return diff;
        }

        PopulateSnapshot(snapshot, playersByLogin, leaderboard, diff);

        return diff;
    }

    private static void PopulateSnapshot(
        TMFGeneralScoresSnapshot snapshot,
        IDictionary<string, TMFLogin> playersByLogin,
        Leaderboard leaderboard,
        TMFGeneralScoreDiff? diff)
    {
        // Prepare a dictionary of logins to ghosts for new/improved records
        var scoreDict = diff?.NewPlayers
            .Concat(diff.ImprovedPlayers.Select(x => x.New))
            .ToDictionary(x => x.Login);

        foreach (var (i, score) in leaderboard.HighScores.Index())
        {
            var playerLogin = playersByLogin[score.Login];

            var player = new TMFGeneralScoresPlayer
            {
                Snapshot = snapshot,
                Player = playerLogin,
                Score = score.Score,
                Rank = score.Rank,
                Order = (byte)i,
            };

            snapshot.Players.Add(player);
        }
    }
}
