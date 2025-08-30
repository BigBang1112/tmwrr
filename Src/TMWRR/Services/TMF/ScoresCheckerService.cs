using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TMWRR.DiscordReport;
using TMWRR.Entities.TMF;
using TMWRR.Exceptions;
using TMWRR.Extensions;
using TMWRR.Models;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public interface IScoresCheckerService
{
    Task<ScoresNumber?> CheckScoresAsync(ScoresNumber? number, CancellationToken cancellationToken);
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

    public async Task<ScoresNumber?> CheckScoresAsync(ScoresNumber? number, CancellationToken cancellationToken)
    {
        ScoresNumber usedNumber;

        if (number.HasValue)
        {
            usedNumber = number.Value;
        }
        else
        {
            logger.LogInformation("No score number provided, fetching the latest one from master server...");
            var scoresInfo = await masterServer.FetchLatestGeneralScoresInfoAsync(EarliestZoneId, cancellationToken: cancellationToken);
            usedNumber = scoresInfo.Number;
        }

        logger.LogInformation("Checking scores for {ScoresNumber}...", usedNumber);

        var pipeline = pipelineProvider.GetPipeline("scores");

        var dateTimeTasks = new Dictionary<Task<DateTimeOffset>, string>
        {
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchGeneralScoresDateTimeAsync(usedNumber, EarliestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), Constants.General },
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLadderScoresDateTimeAsync(usedNumber, EarliestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), Constants.Multi }
        };

        foreach (var campaign in Campaigns)
        {
            dateTimeTasks.Add(pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchCampaignScoresDateTimeAsync(campaign, usedNumber, EarliestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), campaign);
        }

        var scoresDate = default(DateTime?);
        var sbWebhookMessage = new StringBuilder();

        var hasNewCampaignSnapshots = false;
        var allCampaignDiffs = new Dictionary<string, TMFCampaignScoreDiff>();
        var generalDiff = default(TMFGeneralScoreDiff?);

        // This zip stores full score snapshots for debugging purposes and uploads them via webhook
        // It is not stored in the database for now, as the content is very low level and includes unused zones
        await using var ms = new MemoryStream();
        await using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            await foreach (var (scoreType, lastModifiedAtTask) in dateTimeTasks.WhenEachRemove())
            {
                logger.LogInformation("Received {ScoreType} scores, processing...", scoreType);

                var publishedAt = timeProvider.GetUtcNow();

                DateTimeOffset lastModifiedAt;

                try
                {
                    lastModifiedAt = await lastModifiedAtTask;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch {ScoreType} scores.", scoreType);
                    continue;
                }

                logger.LogInformation("{ScoreType} scores were made at {LastModifiedAt}.", scoreType, lastModifiedAt);

                scoresDate = lastModifiedAt.Date;

                var entry = zip.CreateEntry($"{scoreType}.json");
                await using var entryStream = entry.Open();

                // TODO: should be executed per-thread so that each is not bottlenecked
                // But the debug zip is complicating this part
                // though the scores are not all published at once so it might not be necessary
                switch (scoreType)
                {
                    case Constants.General:
                        {
                            var snapshotExists = await scoresSnapshotService.GeneralSnapshotExistsAsync(lastModifiedAt, cancellationToken);

                            if (snapshotExists)
                            {
                                logger.LogInformation("General scores are up to date.");
                                continue; // MUST BE CONTINUE not break, to skip the debug webhook part
                            }

                            logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                            var snapshot = new TMFGeneralScoresSnapshot
                            {
                                CreatedAt = lastModifiedAt,
                                PublishedAt = publishedAt
                            };

                            logger.LogInformation("Downloading general scores...");

                            var generalScores = await masterServer.DownloadGeneralScoresAsync(usedNumber, EarliestZoneId, cancellationToken);
                            
                            var generalDiffTask = generalScoresJobService.ProcessAsync(generalScores.Zones[Constants.World], snapshot, cancellationToken);

                            await Task.WhenAll(
                                generalDiffTask,
                                JsonSerializer.SerializeAsync(entryStream, generalScores, AppJsonContext.Default.GeneralScores, cancellationToken)
                            );

                            generalDiff = await generalDiffTask;

                            if (snapshot.Players.Count == 0 || generalDiff?.PlayerCountDelta != 0)
                            {
                                snapshot.NoChanges = true;
                                logger.LogInformation("No score changes for {ScoreType}.", scoreType);
                            }

                            await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

                            break;
                        }
                    case Constants.Multi:
                        {
                            var snapshotExists = await scoresSnapshotService.LadderSnapshotExistsAsync(lastModifiedAt, cancellationToken);

                            if (snapshotExists)
                            {
                                logger.LogInformation("Ladder scores are up to date.");
                                continue; // MUST BE CONTINUE not break, to skip the debug webhook part
                            }

                            logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                            var snapshot = new TMFLadderScoresSnapshot
                            {
                                CreatedAt = lastModifiedAt,
                                PublishedAt = publishedAt
                            };

                            logger.LogInformation("Downloading ladder scores...");

                            var ladderScores = await masterServer.DownloadLadderScoresAsync(usedNumber, EarliestZoneId, cancellationToken);
                            
                            var ladderDiffTask = ladderScoresJobService.ProcessAsync(ladderScores.Zones[Constants.World], snapshot, cancellationToken);

                            await Task.WhenAll(
                                ladderDiffTask,
                                JsonSerializer.SerializeAsync(entryStream, ladderScores, AppJsonContext.Default.LadderScores, cancellationToken)
                            );

                            var ladderHasChanged = await ladderDiffTask;

                            if (!ladderHasChanged)
                            {
                                snapshot.NoChanges = true;
                                logger.LogInformation("No score changes for {ScoreType}.", scoreType);
                            }

                            await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

                            break;
                        }
                    default:
                        {
                            var snapshotExists = await scoresSnapshotService.CampaignSnapshotExistsAsync(scoreType, lastModifiedAt, cancellationToken);

                            if (snapshotExists)
                            {
                                logger.LogInformation("Campaign scores for {ScoreType} are up to date.", scoreType);
                                continue; // MUST BE CONTINUE not break, to skip the debug webhook part
                            }

                            logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                            hasNewCampaignSnapshots = true;

                            var snapshot = new TMFCampaignScoresSnapshot
                            {
                                CampaignId = scoreType,
                                CreatedAt = lastModifiedAt,
                                PublishedAt = publishedAt
                            };

                            logger.LogInformation("Downloading campaign scores for {ScoreType}...", scoreType);

                            var campaignScores = await masterServer.DownloadCampaignScoresAsync(scoreType, usedNumber, EarliestZoneId, cancellationToken);

                            var campaignDiffsTask = campaignScoresJobService.ProcessAsync(
                                scoreType,
                                campaignScores.Maps,
                                campaignScores.MedalZones[Constants.World],
                                snapshot,
                                cancellationToken);

                            await Task.WhenAll(
                                campaignDiffsTask,
                                JsonSerializer.SerializeAsync(entryStream, campaignScores, AppJsonContext.Default.CampaignScores, cancellationToken)
                            );

                            var campaignDiffs = await campaignDiffsTask;

                            // populate all campaign diffs for reporting
                            foreach (var (mapUid, diff) in campaignDiffs)
                            {
                                if (!diff.IsEmpty)
                                {
                                    allCampaignDiffs[mapUid] = diff;
                                }
                            }

                            // DO NOT USE DIFFS IN THIS COMPARISON because then fresh maps won't be saved in the snapshot
                            // this will rarely hit though cuz player count changes basically everyday
                            if (snapshot.Records.Count == 0 && snapshot.PlayerCounts.Count == 0)
                            {
                                snapshot.NoChanges = true;
                                logger.LogInformation("No score changes for {ScoreType}.", scoreType);
                            }

                            await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

                            break;
                        }
                }

                sbWebhookMessage.AppendLine($"{scoreType}: {Discord.TimestampTag.FromDateTimeOffset(lastModifiedAt)} (available {Discord.TimestampTag.FromDateTimeOffset(publishedAt)})");
            }
        }

        if (hasNewCampaignSnapshots)
        {
            await reportService.ReportAsync(allCampaignDiffs, cancellationToken);
        }

        using var webhook = Sample.CreateWebhook(options.Value.DiscordWebhookUrl);

        if (scoresDate is null)
        {
            await webhook.SendMessageAsync("No scores were processed. Master server is likely having issues.");
            return null;
        }
        
        await webhook.SendFileAsync(new Discord.FileAttachment(ms, $"{scoresDate:yyyyMMdd}.zip"), sbWebhookMessage.ToString());

        return (ScoresNumber)(((int)usedNumber % 6) + 1);
    }

    internal DateTimeOffset ThrowIfOlderThanDay(DateTimeOffset dateTime)
    {
        if (dateTime < timeProvider.GetUtcNow().AddDays(-1.5))
        {
            throw new ScoresOlderThanDayException();
        }

        return dateTime;
    }
}
