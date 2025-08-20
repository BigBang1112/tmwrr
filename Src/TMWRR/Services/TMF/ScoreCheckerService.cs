using ManiaAPI.Xml.TMUF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TMWRR.Data;
using TMWRR.DiscordReport;
using TMWRR.Entities;
using TMWRR.Exceptions;
using TMWRR.Extensions;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public interface IScoreCheckerService
{
    Task<ScoresNumber?> CheckScoresAsync(ScoresNumber? number, CancellationToken cancellationToken);
}

public sealed class ScoreCheckerService : IScoreCheckerService
{
    private const int EarliestZoneId = 5;
    private const int LatestZoneId = 109363;

    private static readonly string[] Campaigns = [
        "UnitedRace",
        "UnitedPuzzle",
        "UnitedPlatform",
        "UnitedStunts",
        "Nations",
        "ManiaStar"
    ];

    private readonly ICampaignScoresJobService campaignScoresJobService;
    private readonly IGeneralScoresJobService generalScoresJobService;
    private readonly MasterServerTMUF masterServer;
    private readonly AppDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly ResiliencePipelineProvider<string> pipelineProvider;
    private readonly IOptionsSnapshot<TMUFOptions> options;
    private readonly ILogger<ScoreCheckerService> logger;

    // TODO: replace with actual DB
    private static readonly Dictionary<string, DateTimeOffset> tempDb = [];

    public ScoreCheckerService(
        ICampaignScoresJobService campaignScoresJobService,
        IGeneralScoresJobService generalScoresJobService,
        MasterServerTMUF masterServer, 
        AppDbContext db,
        TimeProvider timeProvider, 
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptionsSnapshot<TMUFOptions> options, 
        ILogger<ScoreCheckerService> logger)
    {
        this.campaignScoresJobService = campaignScoresJobService;
        this.generalScoresJobService = generalScoresJobService;
        this.masterServer = masterServer;
        this.db = db;
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
                cancellationToken).AsTask(), "General" },
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLadderScoresDateTimeAsync(usedNumber, LatestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), "Multi" }
        };

        foreach (var campaign in Campaigns)
        {
            dateTimeTasks.Add(pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchCampaignScoresDateTimeAsync(campaign, usedNumber, LatestZoneId, cancellationToken: token)),
                cancellationToken).AsTask(), campaign);
        }

        var scoresDate = default(DateTime?);
        var sbWebhookMessage = new StringBuilder();

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

                var snapshotExists = await db.TMUFScoresSnapshots
                    .AnyAsync(x => x.CampaignId == scoreType && x.CreatedAt == lastModifiedAt, cancellationToken);

                if (snapshotExists)
                {
                    logger.LogInformation("The scores for {ScoreType} are up to date.", scoreType);
                    continue;
                }

                await db.TMUFScoresSnapshots.AddAsync(new TMUFScoresSnapshot
                {
                    CampaignId = scoreType,
                    CreatedAt = lastModifiedAt,
                    PublishedAt = publishedAt
                }, cancellationToken);

                logger.LogWarning("New! {ScoreType}: {CreatedAt}", scoreType, lastModifiedAt);

                var entry = zip.CreateEntry($"{scoreType}.json");
                await using var entryStream = entry.Open();

                switch (scoreType)
                {
                    case "General":
                        var generalScores = await masterServer.DownloadGeneralScoresAsync(usedNumber, LatestZoneId, cancellationToken);
                        await Task.WhenAll(
                            JsonSerializer.SerializeAsync(entryStream, generalScores, AppJsonContext.Default.GeneralScores, cancellationToken),
                            generalScoresJobService.ProcessAsync(generalScores.Zones[Constants.World], cancellationToken)
                        );
                        break;
                    case "Multi":
                        var ladderScores = await masterServer.DownloadLadderScoresAsync(usedNumber, LatestZoneId, cancellationToken);
                        await JsonSerializer.SerializeAsync(entryStream, ladderScores, AppJsonContext.Default.LadderScores, cancellationToken);
                        // TODO: only count players?
                        break;
                    default:
                        var campaignScores = await masterServer.DownloadCampaignScoresAsync(scoreType, usedNumber, LatestZoneId, cancellationToken);
                        await Task.WhenAll(
                            JsonSerializer.SerializeAsync(entryStream, campaignScores, AppJsonContext.Default.CampaignScores, cancellationToken),
                            campaignScoresJobService.ProcessAsync(
                                scoreType,
                                campaignScores.Maps,
                                campaignScores.MedalZones[Constants.World],
                                cancellationToken)
                            );
                        break;
                }

                await db.SaveChangesAsync(cancellationToken);

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
