using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TMWRR.DiscordReport;
using TMWRR.Entities;
using TMWRR.Exceptions;
using TMWRR.Extensions;
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
            var scoresInfo = await masterServer.FetchLatestGeneralScoresInfoAsync(LatestZoneId, cancellationToken: cancellationToken);
            usedNumber = scoresInfo.Number;
        }

        var pipeline = pipelineProvider.GetPipeline("scores");

        var dateTimeTasks = new Dictionary<Task<DateTimeOffset>, string>
        {
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchGeneralScoresDateTimeAsync(usedNumber, LatestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), Constants.General },
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLadderScoresDateTimeAsync(usedNumber, LatestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), Constants.Multi }
        };

        foreach (var campaign in Campaigns)
        {
            dateTimeTasks.Add(pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchCampaignScoresDateTimeAsync(campaign, usedNumber, LatestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), campaign);
        }

        var scoresDate = default(DateTime?);
        var sbWebhookMessage = new StringBuilder();

        // This zip stores full score snapshots for debugging purposes and uploads them via webhook
        // It is not stored in the database for now, as the content is very low level and includes unused zones
        await using var ms = new MemoryStream();
        await using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            await foreach (var (scoreType, lastModifiedAtTask) in dateTimeTasks.WhenEachRemove())
            {
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

                var entry = zip.CreateEntry($"{scoreType}.json");
                await using var entryStream = entry.Open();

                // TODO: should be executed per-thread so that each is not bottlenecked
                // But the debug zip is complicating this part
                switch (scoreType)
                {
                    case Constants.General:
                        logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                        var generalScores = await masterServer.DownloadGeneralScoresAsync(usedNumber, LatestZoneId, cancellationToken);
                        await Task.WhenAll(
                            JsonSerializer.SerializeAsync(entryStream, generalScores, AppJsonContext.Default.GeneralScores, cancellationToken),
                            generalScoresJobService.ProcessAsync(generalScores.Zones[Constants.World], cancellationToken)
                        );
                        break;
                    case Constants.Multi:
                        logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                        var ladderScores = await masterServer.DownloadLadderScoresAsync(usedNumber, LatestZoneId, cancellationToken);
                        await Task.WhenAll(
                            JsonSerializer.SerializeAsync(entryStream, ladderScores, AppJsonContext.Default.LadderScores, cancellationToken),
                            ladderScoresJobService.ProcessAsync(ladderScores.Zones[Constants.World], cancellationToken)
                        );
                        // TODO: only count players?
                        break;
                    default:
                        var snapshotExists = await scoresSnapshotService.CampaignSnapshotExistsAsync(scoreType, lastModifiedAt, cancellationToken);

                        if (snapshotExists)
                        {
                            logger.LogInformation("Campaign scores for {ScoreType} are up to date.", scoreType);
                            continue; // MUST BE CONTINUE not break, to skip the debug webhook part
                        }

                        var snapshot = new TMFCampaignScoresSnapshot
                        {
                            CampaignId = scoreType,
                            CreatedAt = lastModifiedAt,
                            PublishedAt = publishedAt
                        };

                        logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                        var campaignScores = await masterServer.DownloadCampaignScoresAsync(scoreType, usedNumber, LatestZoneId, cancellationToken);
                        
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

                        // DO NOT USE DIFFS IN THIS COMPARISON because then fresh maps won't be saved in the snapshot
                        if (snapshot.Records.Count == 0)
                        {
                            snapshot.NoChanges = true;
                            logger.LogInformation("No score changes for {ScoreType}.", scoreType);
                        }

                        await scoresSnapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

                        await reportService.ReportAsync(campaignDiffs, cancellationToken);

                        break;
                }

                sbWebhookMessage.AppendLine($"{scoreType}: {Discord.TimestampTag.FromDateTimeOffset(lastModifiedAt)} (available {Discord.TimestampTag.FromDateTimeOffset(publishedAt)})");

                scoresDate = lastModifiedAt.Date;
            }
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
        if (dateTime < timeProvider.GetUtcNow().AddDays(-1))
        {
            throw new ScoresOlderThanDayException();
        }

        return dateTime;
    }
}
