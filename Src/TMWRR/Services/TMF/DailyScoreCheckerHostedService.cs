using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using TimeZoneConverter;
using TMWRR.Options;

namespace TMWRR.Services.TMF;

public sealed class DailyScoreCheckerHostedService : BackgroundService
{
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
        await RunScoreCheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitForNextCheckAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("The scheduled task was cancelled.");
                break;
            }
        }
    }

    internal async Task WaitForNextCheckAsync(CancellationToken stoppingToken)
    {
        var delay = GetNextCheckDateTime() - timeProvider.GetUtcNow();

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
                return;
            }
        }

        try
        {
            await RunScoreCheckAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred executing the scheduled task.");
        }
    }

    internal async Task RunScoreCheckAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var scoreCheckerService = scope.ServiceProvider.GetRequiredService<IScoresCheckerService>();

        await scoreCheckerService.CheckScoresAsync(cancellationToken);
    }

    internal DateTimeOffset GetNextCheckDateTime()
    {
        using var scope = scopeFactory.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TMUFOptions>>();

        var nowUtc = timeProvider.GetUtcNow();
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, CET);

        var todayCheckLocal = nowLocal.Date + options.Value.CheckTimeOfDayCEST;
        var nextCheckLocal = nowLocal > todayCheckLocal
            ? todayCheckLocal.AddDays(1)
            : todayCheckLocal;

        return TimeZoneInfo.ConvertTimeToUtc(nextCheckLocal, CET);
    }
}
