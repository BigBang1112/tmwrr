using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using TMWRR.Entities;
using TMWRR.Entities.TMF;
using TMWRR.Models;
using TMWRR.Options;

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
    private readonly IReplayService replayService;
    private readonly IOptionsSnapshot<TMUFOptions> options;
    private readonly ILogger<CampaignScoresJobService> logger;

    public CampaignScoresJobService(
        IMapService mapService, 
        ILoginService loginService, 
        IScoresSnapshotService scoresSnapshotService,
        IGhostService ghostService,
        IReplayService replayService,
        IOptionsSnapshot<TMUFOptions> options,
        ILogger<CampaignScoresJobService> logger)
    {
        this.mapService = mapService;
        this.loginService = loginService;
        this.scoresSnapshotService = scoresSnapshotService;
        this.ghostService = ghostService;
        this.replayService = replayService;
        this.options = options;
        this.logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, TMFCampaignScoreDiff>> ProcessAsync(
        string campaignId,
        IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps,
        CampaignScoresMedalZone medals,
        TMFCampaignScoresSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing campaign {CampaignId} with {MapCount} maps...", campaignId, maps.Count);

        var mapsByUid = await mapService.PopulateAsync(maps.Keys, cancellationToken);

        var nicknamesByLogin = maps.Values
            .SelectMany(x => x.ChallengeScores[Constants.World].HighScores)
            .DistinctBy(x => x.Login)
            .ToDictionary(x => x.Login, x => x.Nickname);

        // these could run in parallel
        var playersByLogin = await loginService.PopulateAsync(nicknamesByLogin, options.Value.EnableLoginDetails, cancellationToken);
        
        logger.LogInformation("Fetching previous records and player counts...");
        var prevRecords = await scoresSnapshotService.GetLatestRecordsAsync(mapsByUid.Values, cancellationToken);

        logger.LogInformation("Fetching previous player counts...");
        var prevPlayerCounts = await scoresSnapshotService.GetLatestPlayerCountsAsync(mapsByUid.Values, cancellationToken);
        //

        // map record counts used to calculate skillpoints
        var playerCounts = new Dictionary<string, int>();

        logger.LogInformation("Populating snapshot with player counts...");
        // Populate snapshot with player counts if they are different
        foreach (var (mapUid, leaderboardZones) in maps)
        {
            var map = mapsByUid[mapUid];
            var leaderboard = leaderboardZones.ChallengeScores[Constants.World];

            prevPlayerCounts.TryGetValue(mapUid, out var existingCount);

            var currentCount = leaderboard.Skillpoints.Sum(x => x.Count);
            playerCounts[mapUid] = currentCount;

            logger.LogDebug("Map {MapUid} player count: {Count} (previously {ExistingCount})", mapUid, currentCount, existingCount);

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

        logger.LogInformation("Populating snapshot with records...");

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
                // This ensures that a full leaderboard isn't imported when seeding
                logger.LogInformation("No previous records found for map {MapUid}, populating snapshot with no diff to report...", mapUid);
                await PopulateSnapshotAsync(snapshot, playersByLogin, map, leaderboard, diff: null, cancellationToken);
                continue;
            }

            logger.LogInformation("Calculating diff for {MapUid}...", mapUid);

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

            var diff = TMFCampaignScoreDiff.Calculate(leaderboard, oldByLogin, newByLogin, map.IsStunts());

            if (diff.IsEmpty)
            {
                // No changes, skip snapshot creation
                logger.LogInformation("No changes detected, skipping snapshot data creation for {MapUid}...", mapUid);
                continue;
            }

            diffs[mapUid] = diff;

            logger.LogInformation("Populating snapshot with new data from {MapUid}...", mapUid);

            // be aware this wont populate existing records with ghosts, only new snapshot records
            // for existing records, either demand-based (request=download) or maintenance job solution needed 
            await PopulateSnapshotAsync(snapshot, playersByLogin, map, leaderboard, diff, cancellationToken);
        }

        logger.LogInformation("Returning diffs for {Count} maps...", diffs.Count);

        return diffs;
    }

    private async Task<Ghost?> DownloadGhostAsync(Map map, TMFLogin login, int score, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking existing ghost for map {MapUid} and login {Login}...", map.MapUid, login.Id);

        var existingRecord = await scoresSnapshotService.GetRecordAsync(map, login, score, cancellationToken);
        var ghost = existingRecord?.Ghost;

        if (ghost is null)
        {
            logger.LogDebug("No existing ghost found, attempting to download for map {MapUid} and login {Login}...", map.MapUid, login.Id);

            try
            {
                return await ghostService.CreateGhostAsync(map, login, score, cancellationToken);
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

    private async Task<Replay?> DownloadReplayAsync(Map map, TMFLogin login, int score, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking existing replay for map {MapUid} and login {Login}...", map.MapUid, login.Id);

        var existingRecord = await scoresSnapshotService.GetRecordAsync(map, login, score, cancellationToken);
        var replay = existingRecord?.Replay;

        if (replay is null)
        {
            logger.LogDebug("No existing replay found, attempting to download for map {MapUid} and login {Login}...", map.MapUid, login.Id);

            try
            {
                return await replayService.CreateReplayAsync(map, login, score, cancellationToken);
            }
            catch (Exception ex)
            {
                // in case download resilience fails, just skip this ghost and dont kill the whole job
                logger.LogError(ex, "Failed to create a replay entity for map {MapUid} and login {Login}", map.MapUid, login.Id);
                return null;
            }
        }

        return replay;
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

            var ghost = default(Ghost);
            var replay = default(Replay);

            if (options.Value.EnableGhostDownload)
            {
                if (map.IsPuzzle())
                {
                    replay = await DownloadReplayAsync(map, player, score.Score, cancellationToken);
                }
                else
                {
                    ghost = await DownloadGhostAsync(map, player, score.Score, cancellationToken);
                }
            }

            var record = new TMFCampaignScoresRecord
            {
                Snapshot = snapshot,
                Map = map,
                Player = player,
                Score = score.Score,
                Rank = score.Rank,
                Order = (byte)i,
                Ghost = ghost,
                Replay = replay,
            };

            snapshot.Records.Add(record);

            // Set the timestamp for new/improved records
            if (scoreDict?.TryGetValue(score.Login, out var diffScore) == true)
            {
                if (ghost is not null)
                {
                    diffScore.Timestamp = ghost.LastModifiedAt;
                    diffScore.GhostGuid = ghost.Guid;
                }

                if (replay is not null)
                {
                    diffScore.Timestamp = replay.LastModifiedAt;
                    diffScore.ReplayGuid = replay.Guid;
                }
            }
        }

        //snapshot.PlayerCounts = leaderboard.Skillpoints.Sum(x => x.Count);
    }
}
