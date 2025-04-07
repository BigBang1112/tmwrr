using ManiaAPI.Xml.TMUF;
using Polly;
using TMWRR.DiscordReport;

namespace TMWRR.Services.TMF;

internal interface IScoreCheckerService
{
    Task<ScoresNumber> CheckScoresAsync(ScoresNumber? number, CancellationToken cancellationToken);
}

internal sealed class ScoreCheckerService : IScoreCheckerService
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
    private readonly ILogger<ScoreCheckerService> logger;

    private static Dictionary<string, DateTimeOffset> tempDb = [];

    public ScoreCheckerService(MasterServerTMUF masterServer, TimeProvider timeProvider, ILogger<ScoreCheckerService> logger)
    {
        this.masterServer = masterServer;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    private static readonly ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
        .AddTimeout(TimeSpan.FromHours(1))
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(), // temp
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.FromSeconds(30),
        })
        .Build();

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

        while (dateTimeTasks.Count > 0)
        {
            var task = await Task.WhenAny(dateTimeTasks.Keys);
            var scoreType = dateTimeTasks[task];
            dateTimeTasks.Remove(task);

            var result = await task;

            if (tempDb.TryGetValue(scoreType, out var lastDateTime) && lastDateTime == result)
            {
                logger.LogInformation("The scores for {ScoreType} are up to date.", scoreType);
                continue;
            }

            tempDb[scoreType] = result;

            logger.LogWarning("New! {ScoreType}: {DateTime}", scoreType, result);

            //await Sample.ReportAsync($"{scoreType}: {result}");
        }

        return (ScoresNumber)(((int)usedNumber % 6) + 1);

        //var generalScoresInfo = 

        /*if (generalScoresInfo is null)
        {
            logger.LogWarning("Failed to retrieve info for general scores, because the scores file for this zone doesn't exist.");
            return null;
        }

        if (approxLastModifiedDateTime is null || generalScoresInfo.Value.LastModified > approxLastModifiedDateTime)
        {
            approxLastModifiedDateTime = generalScoresInfo.Value.LastModified;
        }

        var scoresNumber = generalScoresInfo.Value.Number;

        logger.LogInformation("Retrieved scores info for general scores. Number: {Number}, Approx. date: {Date}.", scoresNumber, approxLastModifiedDateTime);

        var generalScores = await masterServer.DownloadGeneralScoresAsync(scoresNumber, EarliestZone, cancellationToken);

        if (generalScores is null)
        {
            throw new InvalidOperationException("Failed to retrieve general scores. The zone was considered existing, but for the download, it doesn't.");
        }

        logger.LogInformation("Retrieved scores for general scores.");

        await ProcessGeneralScoresAsync(generalScores.Zones["World"], cancellationToken);

        foreach (var campaign in Campaigns)
        {
            var campaignScores = await masterServer.DownloadCampaignScoresAsync(campaign, scoresNumber, EarliestZone, cancellationToken);

            if (campaignScores is null)
            {
                logger.LogWarning("Failed to retrieve scores for campaign {Campaign}, because the scores file for this zone doesn't exist.", campaign);
                continue;
            }

            logger.LogInformation("Retrieved scores for campaign {Campaign}.", campaign);

            await ProcessCampaignScoresAsync(campaign, campaignScores.Maps, campaignScores.MedalZones["World"], cancellationToken);
        }

        return approxLastModifiedDateTime;*/
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
            throw new Exception("The scores are older than a day. The scores will be checked again.");
        }
        return dateTime;
    }
}
