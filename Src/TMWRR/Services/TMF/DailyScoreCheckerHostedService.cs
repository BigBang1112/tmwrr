namespace TMWRR.Services.TMF;

internal sealed class DailyScoreCheckerHostedService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DailyScoreCheckerHostedService> logger;

    private static readonly TimeZoneInfo CEST = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    public DailyScoreCheckerHostedService(IServiceProvider serviceProvider, TimeProvider timeProvider, ILogger<DailyScoreCheckerHostedService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Execute the scheduled task immediately when the service starts
        var nextCheckAt = await RunScoreCheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = nextCheckAt - timeProvider.GetUtcNow();

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

        var nextScoreNumber = await scoreCheckerService.CheckScoresAsync(number: null, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var nextCheckTimeUtc = TimeSpan.FromHours(11) - CEST.GetUtcOffset(now);
        var nextCheckDateTime = new DateTimeOffset(now.Date.Add(nextCheckTimeUtc), TimeSpan.Zero);

        if (now.TimeOfDay > nextCheckTimeUtc)
        {
            nextCheckDateTime = nextCheckDateTime.AddDays(1);
        }

        logger.LogInformation("Next check at {NextCheckAt}.", nextCheckDateTime);

        return nextCheckDateTime;
    }
}
