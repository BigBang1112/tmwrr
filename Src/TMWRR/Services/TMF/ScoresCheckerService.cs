using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.Collections.Immutable;
using TMWRR.DiscordReport;
using TMWRR.Entities.TMF;
using TMWRR.Exceptions;
using TMWRR.Extensions;
using TMWRR.Models;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public interface IScoresCheckerService
{
    Task CheckScoresAsync(CancellationToken cancellationToken);
}

public sealed class ScoresCheckerService : IScoresCheckerService
{
    private const int EarliestZoneId = 5;
    private const int LatestZoneId = 109363;

    public static readonly ImmutableArray<string> Campaigns = [
        "UnitedRace",
        "UnitedPuzzle",
        "UnitedPlatform",
        "UnitedStunts",
        "Nations",
        "ManiaStar"
    ];

    private readonly ICampaignScoresJobService campaignScoresJobService;
    private readonly IGeneralScoresJobService generalScoresJobService;
    private readonly ILadderScoresJobService ladderScoresJobService;
    private readonly IScoresSnapshotService scoresSnapshotService;
    private readonly IReportService reportService;
    private readonly MasterServerTMUF masterServer;
    private readonly TimeProvider timeProvider;
    private readonly ResiliencePipelineProvider<string> pipelineProvider;
    private readonly IOptionsSnapshot<TMUFOptions> options;
    private readonly ILogger<ScoresCheckerService> logger;

    public ScoresCheckerService(
        ICampaignScoresJobService campaignScoresJobService,
        IGeneralScoresJobService generalScoresJobService,
        ILadderScoresJobService ladderScoresJobService,
        IScoresSnapshotService scoresSnapshotService,
        IReportService reportService,
        MasterServerTMUF masterServer,
        TimeProvider timeProvider, 
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptionsSnapshot<TMUFOptions> options, 
        ILogger<ScoresCheckerService> logger)
    {
        this.campaignScoresJobService = campaignScoresJobService;
        this.generalScoresJobService = generalScoresJobService;
        this.ladderScoresJobService = ladderScoresJobService;
        this.scoresSnapshotService = scoresSnapshotService;
        this.reportService = reportService;
        this.masterServer = masterServer;
        this.timeProvider = timeProvider;
        this.pipelineProvider = pipelineProvider;
        this.options = options;
        this.logger = logger;
    }

    public async Task CheckScoresAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking scores...");

        var pipeline = pipelineProvider.GetPipeline("scores");

        var scoresInfoTasks = new Dictionary<Task<ScoresInfo>, string>
        {
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLatestGeneralScoresInfoAsync(EarliestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), Constants.General },
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLatestLadderScoresInfoAsync(EarliestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), Constants.Multi }
        };

        foreach (var campaign in Campaigns)
        {
            scoresInfoTasks.Add(pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLatestCampaignScoresInfoAsync(campaign, EarliestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), campaign);
        }

        var usedNumber = default(ScoresNumber?);
        var scoresDate = default(DateTime?);

        var hasNewCampaignSnapshots = false;
        var allCampaignDiffs = new Dictionary<string, TMFCampaignScoreDiff>();
        var recordCountDiffsByCampaignId = new Dictionary<string, PlayerCountDiff>();

        await foreach (var (scoreType, scoresInfoTask) in scoresInfoTasks.WhenEachRemove())
        {
            logger.LogInformation("Received {ScoreType} scores, processing...", scoreType);

            var publishedAt = timeProvider.GetUtcNow();

            ScoresInfo scoresInfo;

            try
            {
                scoresInfo = await scoresInfoTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch {ScoreType} scores.", scoreType);
                continue;
            }

            logger.LogInformation("{ScoreType} scores were made at {LastModifiedAt}.", scoreType, scoresInfo.LastModified);

            usedNumber = scoresInfo.Number;
            scoresDate = scoresInfo.LastModified.Date;

            // TODO: should be executed per-thread so that each is not bottlenecked
            switch (scoreType)
            {
                case Constants.General:
                    await CheckGeneralScoresAsync(usedNumber.Value, publishedAt, scoresInfo.LastModified, cancellationToken);
                    break;
                case Constants.Multi:
                    await CheckLadderScoresAsync(usedNumber.Value, publishedAt, scoresInfo.LastModified, cancellationToken);
                    break;
                default:
                    hasNewCampaignSnapshots = await CheckCampaignScoresAsync(usedNumber.Value, scoreType, allCampaignDiffs, recordCountDiffsByCampaignId, publishedAt, scoresInfo.LastModified, cancellationToken);
                    break;
            }
        }

        if (usedNumber.HasValue)
        {
            logger.LogInformation("Score check for {ScoresNumber} completed.", usedNumber);
        }

        if (hasNewCampaignSnapshots)
        {
            logger.LogInformation("Reporting campaign score changes...");

            await reportService.ReportAsync(allCampaignDiffs, recordCountDiffsByCampaignId, cancellationToken);
        }

        if (scoresDate is null)
        {
            using var webhook = Sample.CreateWebhook(options.Value.Discord.TestWebhookUrl);
            await webhook.SendMessageAsync("No scores were processed. Master server is likely having issues.");
        }
    }

    private async Task CheckGeneralScoresAsync(ScoresNumber number, DateTimeOffset publishedAt, DateTimeOffset lastModifiedAt, CancellationToken cancellationToken)
    {
        var snapshotExists = await scoresSnapshotService.GeneralSnapshotExistsAsync(lastModifiedAt, cancellationToken);

        if (snapshotExists)
        {
            logger.LogInformation("General scores are up to date.");
            return;
        }

        logger.LogWarning("New! {ScoreType}: {CreatedAt}", Constants.General, lastModifiedAt);

        var snapshot = new TMFGeneralScoresSnapshotEntity
        {
            CreatedAt = lastModifiedAt,
            PublishedAt = publishedAt
        };

        logger.LogInformation("Downloading general scores...");

        var generalScores = await masterServer.DownloadGeneralScoresAsync(number, EarliestZoneId, cancellationToken);

        var generalDiff = await generalScoresJobService.ProcessAsync(generalScores[Constants.World], snapshot, cancellationToken);

        if (snapshot.Players.Count == 0 || generalDiff?.PlayerCountDelta == 0)
        {
            snapshot.NoChanges = true;
            logger.LogInformation("No score changes for {ScoreType}.", Constants.General);
        }

        await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

        logger.LogInformation("Reporting general score changes...");

        await reportService.ReportAsync(snapshot, generalDiff, cancellationToken);
    }

    private async Task CheckLadderScoresAsync(ScoresNumber number, DateTimeOffset publishedAt, DateTimeOffset lastModifiedAt, CancellationToken cancellationToken)
    {
        var snapshotExists = await scoresSnapshotService.LadderSnapshotExistsAsync(lastModifiedAt, cancellationToken);

        if (snapshotExists)
        {
            logger.LogInformation("Ladder scores are up to date.");
            return;
        }

        logger.LogWarning("New! {ScoreType}: {CreatedAt}", Constants.Multi, lastModifiedAt);

        var snapshot = new TMFLadderScoresSnapshotEntity
        {
            CreatedAt = lastModifiedAt,
            PublishedAt = publishedAt
        };

        logger.LogInformation("Downloading ladder scores...");

        var ladderScores = await masterServer.DownloadLadderScoresAsync(number, EarliestZoneId, cancellationToken);

        var ladderHasChanged = await ladderScoresJobService.ProcessAsync(ladderScores[Constants.World], snapshot, cancellationToken);

        if (!ladderHasChanged)
        {
            snapshot.NoChanges = true;
            logger.LogInformation("No score changes for {ScoreType}.", Constants.Multi);
        }

        await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);
    }

    private async Task<bool> CheckCampaignScoresAsync(
        ScoresNumber number, 
        string scoreType, 
        Dictionary<string, TMFCampaignScoreDiff> allCampaignDiffsByMapUid,
        Dictionary<string, PlayerCountDiff> recordCountDiffsByCampaignId,
        DateTimeOffset publishedAt, 
        DateTimeOffset lastModifiedAt,
        CancellationToken cancellationToken)
    {
        var snapshotExists = await scoresSnapshotService.CampaignSnapshotExistsAsync(scoreType, lastModifiedAt, cancellationToken);

        if (snapshotExists)
        {
            logger.LogInformation("Campaign scores for {ScoreType} are up to date.", scoreType);
            return false;
        }

        logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

        var snapshot = new TMFCampaignScoresSnapshotEntity
        {
            CampaignId = scoreType,
            CreatedAt = lastModifiedAt,
            PublishedAt = publishedAt
        };

        logger.LogInformation("Downloading campaign scores for {ScoreType}...", scoreType);

        var campaignScores = await masterServer.DownloadCampaignScoresAsync(scoreType, number, EarliestZoneId, cancellationToken);

        var (campaignDiffs, playerDiff) = await campaignScoresJobService.ProcessAsync(
            scoreType,
            campaignScores[0].ChallengeScores,
            campaignScores[0].MedalLeagues.First(x => x.Name == Constants.World),
            snapshot,
            cancellationToken);

        // populate all campaign diffs for reporting
        foreach (var (mapUid, diff) in campaignDiffs)
        {
            if (!diff.IsEmpty)
            {
                allCampaignDiffsByMapUid[mapUid] = diff;
            }
        }

        recordCountDiffsByCampaignId[scoreType] = playerDiff;

        // DO NOT USE DIFFS IN THIS COMPARISON because then fresh maps won't be saved in the snapshot
        // this will rarely hit though cuz player count changes basically everyday
        if (snapshot.Records.Count == 0 && snapshot.PlayerCounts.Count == 0)
        {
            snapshot.NoChanges = true;
            logger.LogInformation("No score changes for {ScoreType}.", scoreType);
        }

        await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

        return true; // hasNewCampaignSnapshots
    }

    internal ScoresInfo ThrowIfOlderThanDay(ScoresInfo scoresInfo)
    {
        if (scoresInfo.LastModified < timeProvider.GetUtcNow().AddDays(-1))
        {
            throw new ScoresOlderThanDayException();
        }

        return scoresInfo;
    }
}
