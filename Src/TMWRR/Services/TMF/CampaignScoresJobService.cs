using ManiaAPI.Xml.TMUF;
using TMWRR.Entities;
using TMWRR.Entities.TMF;
using TMWRR.Models;

namespace TMWRR.Services.TMF;

public interface ICampaignScoresJobService
{
    Task<IReadOnlyDictionary<string, TMFCampaignScoreDiff>> ProcessAsync(
        string campaignId, 
        IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps, 
        CampaignScoresMedalZone medals, 
        TMFCampaignScoresSnapshot snapshot, 
        CancellationToken cancellationToken);
}

public class CampaignScoresJobService : ICampaignScoresJobService
{
    private readonly IMapService mapService;
    private readonly ILoginService loginService;
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly IGhostService ghostService;
    private readonly ILogger<CampaignScoresJobService> logger;

    public CampaignScoresJobService(
        IMapService mapService, 
        ILoginService loginService, 
        IScoresSnapshotService scoresSnapshotService,
        IGhostService ghostService,
        ILogger<CampaignScoresJobService> logger)
    {
        this.mapService = mapService;
        this.loginService = loginService;
        this.scoresSnapshotService = scoresSnapshotService;
        this.ghostService = ghostService;
        this.logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, TMFCampaignScoreDiff>> ProcessAsync(
        string campaignId,
        IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps,
        CampaignScoresMedalZone medals,
        TMFCampaignScoresSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var mapsByUid = await mapService.PopulateAsync(maps.Keys, cancellationToken);

        // these could run in parallel
        var nicknamesByLogin = maps.Values
            .SelectMany(x => x.ChallengeScores[Constants.World].HighScores)
            .DistinctBy(x => x.Login)
            .ToDictionary(x => x.Login, x => x.Nickname);
        var playersByLogin = await loginService.PopulateAsync(nicknamesByLogin, cancellationToken);
        var prevRecords = await scoresSnapshotService.GetLatestRecordsAsync(mapsByUid.Values, cancellationToken);
        var prevPlayerCounts = await scoresSnapshotService.GetLatestPlayerCountsAsync(mapsByUid.Values, cancellationToken);
        //

        // map record counts used to calculate skillpoints
        var playerCounts = new Dictionary<string, int>();

        // Populate snapshot with player counts if they are different
        foreach (var (mapUid, leaderboardZones) in maps)
        {
            var map = mapsByUid[mapUid];
            var leaderboard = leaderboardZones.ChallengeScores[Constants.World];

            prevPlayerCounts.TryGetValue(mapUid, out var existingCount);

            var currentCount = leaderboard.Skillpoints.Sum(x => x.Count);

            playerCounts[mapUid] = currentCount;

            if (existingCount == currentCount)
            {
                logger.LogInformation("Skipping player count for map {MapUid} as it is unchanged ({Count})", mapUid, currentCount);
                continue;
            }

            snapshot.PlayerCounts.Add(new TMFCampaignScoresPlayerCount
            {
                Snapshot = snapshot,
                Map = map,
                Count = currentCount,
            });
        }

        var diffs = new Dictionary<string, TMFCampaignScoreDiff>();

        foreach (var (mapUid, leaderboardZones) in maps)
        {
            var map = mapsByUid[mapUid];
            var leaderboard = leaderboardZones.ChallengeScores[Constants.World];

            var existingRecords = prevRecords
                .Where(x => x.Map.MapUid == mapUid)
                .OrderBy(x => x.Order)
                .ToList();

            if (existingRecords.Count == 0)
            {
                // Doesn't count towards the diff, but we still need to populate the snapshot
                await PopulateSnapshotAsync(snapshot, playersByLogin, map, leaderboard, diff: null, cancellationToken);
                continue;
            }

            var prevPlayerCount = prevPlayerCounts.TryGetValue(mapUid, out var count) ? count : default(int?);
            var prevSkillpointRanks = SkillpointCalculator.GetRanksForSkillpoints(existingRecords.Select(x => x.Rank).ToArray());

            var newPlayerCount = playerCounts[mapUid];
            var newSkillpointRanks = SkillpointCalculator.GetRanksForSkillpoints(leaderboard.HighScores.Select(x => x.Rank).ToArray());

            var oldByLogin = existingRecords.Index().ToDictionary(
                r => r.Item.PlayerId, 
                r => new TMFCampaignScore(r.Item.Rank, r.Item.Score, r.Item.PlayerId, prevPlayerCount.HasValue ? SkillpointCalculator.CalculateSkillpoints(prevPlayerCount.Value, prevSkillpointRanks[r.Index]) : null));
            var newByLogin = leaderboard.HighScores.Index().ToDictionary(
                r => r.Item.Login, 
                r => new TMFCampaignScore(r.Item.Rank, r.Item.Score, r.Item.Login, SkillpointCalculator.CalculateSkillpoints(newPlayerCount, newSkillpointRanks[r.Index])));

            var diff = new TMFCampaignScoreDiff();

            // Detect new and improved/worsened
            foreach (var (login, updated) in newByLogin)
            {
                if (!oldByLogin.TryGetValue(login, out var old))
                {
                    // New record
                    diff.NewRecords.Add(updated);
                    continue;
                }

                // Compare by rank first, then by score if needed
                if (updated.Rank < old.Rank || (map.IsStunts() ? updated.Score > old.Score : updated.Score < old.Score))
                {
                    diff.ImprovedRecords.Add((old, updated));
                }
                else if (updated.Rank > old.Rank || (map.IsStunts() ? updated.Score < old.Score : updated.Score > old.Score))
                {
                    diff.WorsenedRecords.Add((old, updated));
                }
            }

            // Maybe just checking last record is enough?
            var worstScore = map.IsStunts()
                ? leaderboard.HighScores.Min(x => x.Score)
                : leaderboard.HighScores.Max(x => x.Score);

            // Detect removed or pushed off
            foreach (var (login, old) in oldByLogin)
            {
                if (newByLogin.ContainsKey(login))
                {
                    continue;
                }

                if (map.IsStunts() ? old.Score <= worstScore : old.Score >= worstScore)
                {
                    diff.PushedOffRecords.Add(old);
                }
                else
                {
                    diff.RemovedRecords.Add(old);
                }
            }

            if (diff.IsEmpty)
            {
                // No changes, skip snapshot creation
                continue;
            }

            diffs[mapUid] = diff;

            // be aware this wont populate existing records with ghosts, only new snapshot records
            // for existing records, either demand-based (request=download) or maintenance job solution needed 
            await PopulateSnapshotAsync(snapshot, playersByLogin, map, leaderboard, diff, cancellationToken);
        }

        return diffs;
    }

    private async Task<Ghost?> DownloadGhostAsync(Map map, TMFLogin login, int score, CancellationToken cancellationToken)
    {
        var existingRecord = await scoresSnapshotService.GetRecordAsync(map, login, score, cancellationToken);
        var ghost = existingRecord?.Ghost;

        if (ghost is null)
        {
            try
            {
                return await ghostService.CreateGhostAsync(map, login, cancellationToken);
            }
            catch (Exception ex)
            {
                // in case download resilience fails, just skip this ghost and dont kill the whole job
                logger.LogError(ex, "Failed to create a ghost entity for map {MapUid} and login {Login}", map.MapUid, login.Id);
                return null;
            }
        }

        return ghost;
    }

    private async Task PopulateSnapshotAsync(
        TMFCampaignScoresSnapshot snapshot, 
        IDictionary<string, TMFLogin> playersByLogin, 
        Map map,
        Leaderboard leaderboard, 
        TMFCampaignScoreDiff? diff,
        CancellationToken cancellationToken)
    {
        // Prepare a dictionary of logins to ghosts for new/improved records
        var scoreDict = diff?.NewRecords
            .Concat(diff.ImprovedRecords.Select(x => x.New))
            .ToDictionary(x => x.Login);

        foreach (var (i, score) in leaderboard.HighScores.Index())
        {
            var player = playersByLogin[score.Login];

            var ghost = await DownloadGhostAsync(map, player, score.Score, cancellationToken);

            var record = new TMFCampaignScoresRecord
            {
                Snapshot = snapshot,
                Map = map,
                Player = player,
                Score = score.Score,
                Rank = score.Rank,
                Order = (byte)i,
                Ghost = ghost,
            };

            snapshot.Records.Add(record);

            // Set the timestamp for new/improved records
            if (ghost is not null && scoreDict?.TryGetValue(score.Login, out var diffScore) == true)
            {
                diffScore.Timestamp = ghost.LastModifiedAt;
                diffScore.GhostGuid = ghost.Guid;
            }
        }

        //snapshot.PlayerCounts = leaderboard.Skillpoints.Sum(x => x.Count);
    }
}
