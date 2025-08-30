using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using TMWRR.Entities.TMF;
using TMWRR.Models;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public interface IGeneralScoresJobService
{
    Task<TMFGeneralScoreDiff?> ProcessAsync(Leaderboard leaderboard, TMFGeneralScoresSnapshot snapshot, CancellationToken cancellationToken);
}

public class GeneralScoresJobService : IGeneralScoresJobService
{
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly ILoginService loginService;
    private readonly IOptionsSnapshot<TMUFOptions> options;
    private readonly ILogger<GeneralScoresJobService> logger;

    public GeneralScoresJobService(
        IScoresSnapshotService scoresSnapshotService, 
        ILoginService loginService,
        IOptionsSnapshot<TMUFOptions> options,
        ILogger<GeneralScoresJobService> logger)
    {
        this.scoresSnapshotService = scoresSnapshotService;
        this.loginService = loginService;
        this.options = options;
        this.logger = logger;
    }

    public async Task<TMFGeneralScoreDiff?> ProcessAsync(
        Leaderboard leaderboard, 
        TMFGeneralScoresSnapshot snapshot, 
        CancellationToken cancellationToken)
    {
        snapshot.PlayerCount = leaderboard.Skillpoints.Sum(x => x.Count);

        logger.LogInformation("Player count: {Count}", snapshot.PlayerCount);

        var nicknamesByLogin = leaderboard.HighScores
            .DistinctBy(x => x.Login)
            .ToDictionary(x => x.Login, x => x.Nickname);

        logger.LogInformation("Gathering {Count} unique logins...", nicknamesByLogin.Count);

        var playersByLogin = await loginService.PopulateAsync(nicknamesByLogin, options.Value.EnableLoginDetails, cancellationToken);

        logger.LogInformation("Fetching previous snapshot...");

        var prevSnapshot = await scoresSnapshotService.GetLatestGeneralSnapshotAsync(cancellationToken);

        if (prevSnapshot is null)
        {
            logger.LogInformation("No previous snapshot found, populating current snapshot with no diff to report...");
            PopulateSnapshot(snapshot, playersByLogin, leaderboard);
            return null;
        }

        logger.LogInformation("Calculating diff...");

        var oldByLogin = prevSnapshot.Players
            .ToDictionary(r => r.PlayerId, r => new TMFGeneralScore(r.Rank, r.Score, r.PlayerId));
        var newByLogin = leaderboard.HighScores.ToDictionary(r => r.Login, r => new TMFGeneralScore(r.Rank, r.Score, r.Login));

        var diff = TMFGeneralScoreDiff.Calculate(leaderboard, oldByLogin, newByLogin);
        diff.PlayerCountDelta = snapshot.PlayerCount - prevSnapshot.PlayerCount; // this is weird mutation but idk how else to do it

        if (diff.IsEmpty)
        {
            // No changes, skip snapshot data creation (inside the snapshot, snapshot is still created but empty)
            logger.LogInformation("No changes detected, skipping snapshot data creation...");
            return diff;
        }

        logger.LogInformation("Populating snapshot with new data...");

        PopulateSnapshot(snapshot, playersByLogin, leaderboard);

        logger.LogInformation("Returning diff...");

        return diff;
    }

    private static void PopulateSnapshot(
        TMFGeneralScoresSnapshot snapshot,
        IDictionary<string, TMFLogin> playersByLogin,
        Leaderboard leaderboard)
    {
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
