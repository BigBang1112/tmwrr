using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TMWRR.DiscordReport;
using TMWRR.Exceptions;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public interface IScoreCheckerService
{
    Task<ScoresNumber> CheckScoresAsync(ScoresNumber? number, CancellationToken cancellationToken);
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

    private readonly MasterServerTMUF masterServer;
    private readonly TimeProvider timeProvider;
    private readonly ResiliencePipelineProvider<string> pipelineProvider;
    private readonly TMUFOptions options;
    private readonly IConfiguration config;
    private readonly ILogger<ScoreCheckerService> logger;

    private static readonly Dictionary<string, DateTimeOffset> tempDb = [];

    public ScoreCheckerService(
        MasterServerTMUF masterServer, 
        TimeProvider timeProvider, 
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptions<TMUFOptions> options, 
        IConfiguration config, 
        ILogger<ScoreCheckerService> logger)
    {
        this.masterServer = masterServer;
        this.timeProvider = timeProvider;
        this.pipelineProvider = pipelineProvider;
        this.options = options.Value;
        this.config = config;
        this.logger = logger;
    }

    public async Task<ScoresNumber> CheckScoresAsync(ScoresNumber? number, CancellationToken cancellationToken)
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
                async token => ThrowIfOlderThanDay(await masterServer.FetchGeneralScoresDateTimeAsync(usedNumber, LatestZoneId, token)),
                cancellationToken).AsTask(), "General" },
            { pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchLadderScoresDateTimeAsync(usedNumber, LatestZoneId, token)),
                cancellationToken).AsTask(), "Multi" }
        };

        foreach (var campaign in Campaigns)
        {
            dateTimeTasks.Add(pipeline.ExecuteAsync(
                async token => ThrowIfOlderThanDay(await masterServer.FetchCampaignScoresDateTimeAsync(campaign, usedNumber, LatestZoneId, token)),
                cancellationToken).AsTask(), campaign);
        }

        var scoresDate = default(DateTime);
        var sb = new StringBuilder();

        await using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            while (dateTimeTasks.Count > 0)
            {
                var task = await Task.WhenAny(dateTimeTasks.Keys);
                var publishedAt = timeProvider.GetUtcNow();
                var scoreType = dateTimeTasks[task];
                dateTimeTasks.Remove(task);

                DateTimeOffset result;

                try
                {
                    result = await task;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch {ScoreType} scores.", scoreType);
                    continue;
                }

                if (tempDb.TryGetValue(scoreType, out var lastDateTime) && lastDateTime == result)
                {
                    logger.LogInformation("The scores for {ScoreType} are up to date.", scoreType);
                    continue;
                }

                tempDb[scoreType] = result;

                logger.LogWarning("New! {ScoreType}: {DateTime}", scoreType, result);

                var entry = zip.CreateEntry($"{scoreType}.json");
                await using var entryStream = entry.Open();

                switch (scoreType)
                {
                    case "General":
                        var generalScores = await masterServer.DownloadGeneralScoresAsync(usedNumber, LatestZoneId, cancellationToken);
                        await JsonSerializer.SerializeAsync(entryStream, generalScores, AppJsonContext.Default.GeneralScores, cancellationToken);
                        break;
                    case "Multi":
                        var ladderScores = await masterServer.DownloadLadderScoresAsync(usedNumber, LatestZoneId, cancellationToken);
                        await JsonSerializer.SerializeAsync(entryStream, ladderScores, AppJsonContext.Default.LadderScores, cancellationToken);
                        break;
                    default:
                        var campaignScores = await masterServer.DownloadCampaignScoresAsync(scoreType, usedNumber, LatestZoneId, cancellationToken);
                        await JsonSerializer.SerializeAsync(entryStream, campaignScores, AppJsonContext.Default.CampaignScores, cancellationToken);
                        break;
                }

                sb.AppendLine($"{scoreType}: {Discord.TimestampTag.FromDateTimeOffset(result)} (available {Discord.TimestampTag.FromDateTimeOffset(publishedAt)})");

                scoresDate = result.Date;
            }
        }

        using var webhook = Sample.CreateWebhook(config["WebhookUrl"]!);
        
        await webhook.SendFileAsync(new Discord.FileAttachment(ms, $"{scoresDate:yyyyMMdd}.zip"), sb.ToString());

        return (ScoresNumber)(((int)usedNumber % 6) + 1);
    }

    private async Task ProcessGeneralScoresAsync(Leaderboard leaderboard, CancellationToken cancellationToken)
    {

    }

    private async Task ProcessCampaignScoresAsync(
        string campaign, 
        IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps, 
        CampaignScoresMedalZone medalsWorld, 
        CancellationToken cancellationToken)
    {

    }

    private DateTimeOffset ThrowIfOlderThanDay(DateTimeOffset dateTime)
    {
        if (dateTime < timeProvider.GetUtcNow().AddDays(-1))
        {
            throw new ScoresOlderThanDayException();
        }

        return dateTime;
    }
}
