namespace TMWRR.Services.TMF;

internal sealed class DailyScoreCheckerHostedService : BackgroundService
{
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

        var scoreCheckerService = scope.ServiceProvider.GetRequiredService<IScoreCheckerService>();

        var approxLastModifiedDateTime = await scoreCheckerService.CheckScoresAsync(cancellationToken);

        if (approxLastModifiedDateTime is null)
        {
            logger.LogError("No scores info retrieved for any campaign. This skips the check for today, and runs it again at 4am.");
            return DateTime.Today.AddDays(1).AddHours(4);
        }

        var nextCheckAt = approxLastModifiedDateTime.Value.AddDays(1).AddMinutes(10);

        logger.LogInformation("Next check at {NextCheckAt}.", nextCheckAt);

        return nextCheckAt;
    }
}
