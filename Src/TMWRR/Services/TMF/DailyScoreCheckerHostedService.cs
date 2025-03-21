using ManiaAPI.XmlRpc.TMUF;

namespace TMWRR.Services.TMF;

internal sealed class DailyScoreCheckerHostedService : BackgroundService
{
    private const string EarliestZone = "World|Japan";

    private static readonly string[] Campaigns = [
        "UnitedRace",
        "UnitedPuzzle",
        "UnitedPlatform",
        "UnitedStunts",
        "Nations",
        "ManiaStar"
    ];

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DailyScoreCheckerHostedService> logger;

    public DailyScoreCheckerHostedService(IServiceProvider serviceProvider, ILogger<DailyScoreCheckerHostedService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Execute the scheduled task immediately when the service starts
        var nextCheckAt = await RunScoreCheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = nextCheckAt - DateTimeOffset.UtcNow;

            if (delay < TimeSpan.Zero)
            {
                logger.LogWarning("The scheduled task is running late. Running it immediately.");
            }
            else
            {
                logger.LogInformation("The scheduled task will run in {Delay}.", delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            try
            {
                nextCheckAt = await RunScoreCheckAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred executing the scheduled task.");
            }
        }
    }

    internal async Task<DateTimeOffset> RunScoreCheckAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var masterServer = scope.ServiceProvider.GetRequiredService<MasterServerTMUF>();

        var latestModifiedDateTime = default(DateTimeOffset?);

        foreach (var campaign in Campaigns)
        {
            var scoresInfo = await masterServer.FetchLatestCampaignScoresInfoAsync(campaign, EarliestZone, cancellationToken: cancellationToken);

            if (scoresInfo is null)
            {
                logger.LogWarning("Failed to retrieve scores info for campaign {Campaign}. Unknown reason.", campaign);
                continue;
            }

            if (latestModifiedDateTime is null || scoresInfo.Value.LastModified > latestModifiedDateTime)
            {
                latestModifiedDateTime = scoresInfo.Value.LastModified;
            }

            logger.LogInformation("Retrieved scores info for campaign {Campaign}. Number: {Number}, Date: {Date}.", campaign, scoresInfo.Value.Number, scoresInfo.Value.LastModified);

            var campaignScores = await masterServer.DownloadCampaignScoresAsync(campaign, scoresInfo.Value.Number, EarliestZone, cancellationToken);

            if (campaignScores is null)
            {
                logger.LogWarning("Failed to retrieve scores for campaign {Campaign}. Unknown reason.", campaign);
                continue;
            }

            logger.LogInformation("Retrieved scores for campaign {Campaign}.", campaign);
        }
        
        var generalScoresInfo = await masterServer.FetchLatestGeneralScoresInfoAsync(EarliestZone, cancellationToken: cancellationToken);

        if (generalScoresInfo is null)
        {
            logger.LogWarning("Failed to retrieve scores info for general scores. Unknown reason.");
        }
        else
        {
            if (latestModifiedDateTime is null || generalScoresInfo.Value.LastModified > latestModifiedDateTime)
            {
                latestModifiedDateTime = generalScoresInfo.Value.LastModified;
            }

            logger.LogInformation("Retrieved scores info for general scores. Number: {Number}, Date: {Date}.",
                generalScoresInfo.Value.Number, generalScoresInfo.Value.LastModified);

            var generalScores =
                await masterServer.DownloadGeneralScoresAsync(generalScoresInfo.Value.Number, EarliestZone,
                    cancellationToken);

            if (generalScores is null)
            {
                logger.LogWarning("Failed to retrieve scores for general scores. Unknown reason.");
            }
            else
            {
                logger.LogInformation("Retrieved scores for general scores.");
            }
        }

        logger.LogInformation("Scheduled task executed.");

        if (latestModifiedDateTime is null)
        {
            logger.LogError("No scores info retrieved for any campaign. This skips the check for today, and runs it again at 4am.");
            return DateTime.Today.AddDays(1).AddHours(4);
        }

        var nextCheckAt = latestModifiedDateTime.Value.AddDays(1)
            .AddHours(1)
            .AddMinutes(-latestModifiedDateTime.Value.Minute)
            .AddSeconds(-latestModifiedDateTime.Value.Second);

        logger.LogInformation("Next check at {NextCheckAt}.", nextCheckAt);

        return nextCheckAt;
    }
}
