using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using TimeZoneConverter;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public sealed class DailyScoreCheckerHostedService : BackgroundService
{
    internal readonly record struct ScoresResult(DateTimeOffset NextCheckAt, ScoresNumber? NextNumber);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IDelayService delayService;
    private readonly TimeProvider timeProvider;
    private readonly IOptions<TMUFOptions> options;
    private readonly ILogger<DailyScoreCheckerHostedService> logger;

    private static readonly TimeZoneInfo CET = TZConvert.GetTimeZoneInfo("CET");

    public DailyScoreCheckerHostedService(
        IServiceScopeFactory scopeFactory,
        IDelayService delayService,
        TimeProvider timeProvider, 
        IOptions<TMUFOptions> options,
        ILogger<DailyScoreCheckerHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.delayService = delayService;
        this.timeProvider = timeProvider;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableSoloReport)
        {
            logger.LogInformation("The TMUF Solo daily score checker is disabled.");
            return;
        }

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

    internal async Task<ScoresResult?> WaitForNextCheckAsync(DateTimeOffset nextCheckAt, ScoresNumber? nextNumber, CancellationToken stoppingToken)
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
                await delayService.DelayAsync(delay, stoppingToken);
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

        // its better to look for the latest score again by full scan, so that it can recognize either stucked same scores file, or a skipped one
        return new ScoresResult(GetNextCheckDateTime(), NextNumber: null);
    }

    internal async Task<ScoresResult> RunScoreCheckAsync(ScoresNumber? number, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var scoreCheckerService = scope.ServiceProvider.GetRequiredService<IScoresCheckerService>();

        var nextScoreNumber = await scoreCheckerService.CheckScoresAsync(number, cancellationToken);

        var nextCheckDateTime = GetNextCheckDateTime();

        logger.LogInformation("Next check at {NextCheckAt} on {ScoreNumber}.", nextCheckDateTime, nextScoreNumber);

        return new ScoresResult(nextCheckDateTime, nextScoreNumber);
    }

    internal DateTimeOffset GetNextCheckDateTime()
    {
        using var scope = scopeFactory.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TMUFOptions>>();

        var now = timeProvider.GetUtcNow();
        var nextCheckTimeUtc = options.Value.CheckTimeOfDayCEST - CET.GetUtcOffset(now);
        return new DateTimeOffset(now.Date.Add(nextCheckTimeUtc), TimeSpan.Zero)
            .AddDays(now.TimeOfDay > nextCheckTimeUtc ? 1 : 0);
    }
}
