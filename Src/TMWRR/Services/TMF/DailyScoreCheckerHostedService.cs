using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

internal sealed class DailyScoreCheckerHostedService : BackgroundService
{
    internal readonly record struct ScoresResult(DateTimeOffset NextCheckAt, ScoresNumber NextNumber);

    private readonly IServiceProvider serviceProvider;
    private readonly TimeProvider timeProvider;
    private readonly IOptions<TMUFOptions> options;
    private readonly ILogger<DailyScoreCheckerHostedService> logger;

    public DailyScoreCheckerHostedService(IServiceProvider serviceProvider, TimeProvider timeProvider, IOptions<TMUFOptions> options, ILogger<DailyScoreCheckerHostedService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.timeProvider = timeProvider;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Execute the scheduled task immediately when the service starts
        var (nextCheckAt, nextNumber) = await RunScoreCheckAsync(number: null, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await WaitForNextCheckAsync(nextCheckAt, nextNumber, stoppingToken);

            if (result is null)
            {
                logger.LogInformation("The scheduled task was cancelled.");
                break;
            }

            (nextCheckAt, nextNumber) = result.Value;
        }
    }

    internal async Task<ScoresResult?> WaitForNextCheckAsync(DateTimeOffset nextCheckAt, ScoresNumber nextNumber, CancellationToken stoppingToken)
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
                return null;
            }
        }

        try
        {
            return await RunScoreCheckAsync(nextNumber, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred executing the scheduled task.");
        }

        // the same scores number will be attempted repeatedly
        return new ScoresResult(GetNextCheckDateTime(), nextNumber);
    }

    internal async Task<ScoresResult> RunScoreCheckAsync(ScoresNumber? number, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var scoreCheckerService = scope.ServiceProvider.GetRequiredService<IScoreCheckerService>();

        var nextScoreNumber = await scoreCheckerService.CheckScoresAsync(number, cancellationToken);

        var nextCheckDateTime = GetNextCheckDateTime();

        logger.LogInformation("Next check at {NextCheckAt} on {ScoreNumber}.", nextCheckDateTime, nextScoreNumber);

        return new ScoresResult(nextCheckDateTime, nextScoreNumber);
    }

    internal DateTimeOffset GetNextCheckDateTime()
    {
        var now = timeProvider.GetUtcNow();
        var nextCheckTimeUtc = options.Value.CheckTimeOfDay;
        return new DateTimeOffset(now.Date.Add(nextCheckTimeUtc), TimeSpan.Zero)
            .AddDays(now.TimeOfDay > nextCheckTimeUtc ? 1 : 0);
    }
}
