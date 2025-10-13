using Discord;
using ManiaAPI.Xml.MP4;
using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TMWRR.DiscordReport;
using TMWRR.Options;

namespace TMWRR.Services.TM2;

public sealed class TM2LeaderboardCheckerHostedService : BackgroundService
{
    internal readonly record struct ScoresResult(DateTimeOffset NextCheckAt, ScoresNumber? NextNumber);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IDelayService delayService;
    private readonly TimeProvider timeProvider;
    private readonly IDiscordWebhookFactory webhookFactory;
    private readonly ILogger<TM2LeaderboardCheckerHostedService> logger;

    public TM2LeaderboardCheckerHostedService(
        IServiceScopeFactory scopeFactory,
        IDelayService delayService,
        TimeProvider timeProvider,
        IDiscordWebhookFactory webhookFactory,
        ILogger<TM2LeaderboardCheckerHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.delayService = delayService;
        this.timeProvider = timeProvider;
        this.webhookFactory = webhookFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Execute the scheduled task immediately when the service starts
        var nextCheckAt = await RunLeaderboardCheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await WaitForNextCheckAsync(nextCheckAt, stoppingToken);

            if (result is null)
            {
                //logger.LogInformation("The scheduled task was cancelled.");
                break;
            }

            nextCheckAt = result.Value;
        }
    }

    internal async Task<DateTimeOffset?> WaitForNextCheckAsync(DateTimeOffset nextCheckAt, CancellationToken stoppingToken)
    {
        var delay = nextCheckAt - timeProvider.GetUtcNow();

        if (delay < TimeSpan.Zero)
        {
            //logger.LogWarning("The scheduled task is running late. Running it immediately.");
        }
        else
        {
            //logger.LogInformation("The scheduled task will run in {Delay}.", delay);

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
            return await RunLeaderboardCheckAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            //logger.LogError(ex, "An error occurred executing the scheduled task.");
        }

        return GetNextCheckDateTime();
    }

    private DateTimeOffset? lastTimestamp;

    internal async Task<DateTimeOffset> RunLeaderboardCheckAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TM2Options>>();
        var masterServerMP4 = scope.ServiceProvider.GetRequiredService<MasterServerMP4>();

        var response = await masterServerMP4.GetMapLeaderBoardSummariesResponseAsync("TMStadium@nadeo", [new MapSummaryRequest("Ye6btWgKsS2M4vCqGsL8COTOUoh")], cancellationToken);

        if (response.Result.Count > 0)
        {
            var timestamp = response.Result[0].Timestamp;

            if (lastTimestamp != timestamp)
            {
                lastTimestamp = timestamp;

                using var webhook = webhookFactory.Create(options.Value.Discord.TestWebhookUrl);
                await webhook.SendMessageAsync(new EmbedBuilder
                {
                    Title = "Stadium A01 leaderboard request (relay02)",
                    Timestamp = timestamp,
                    Fields = [
                        new EmbedFieldBuilder
                        {
                            Name = "Timestamp",
                            Value = $"{TimestampTag.FromDateTimeOffset(timestamp, TimestampTagStyles.LongTime)} ({timestamp.ToUnixTimeSeconds()})",
                            IsInline = true,
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Request time",
                            Value = response.Details.RequestTime.TotalSeconds + "s",
                            IsInline = true,
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Execution time",
                            Value = response.ExecutionTime?.TotalSeconds + "s",
                            IsInline = true,
                        },
                    ],
                }.Build(), cancellationToken);
            }
        }

        return GetNextCheckDateTime();
    }

    internal DateTimeOffset GetNextCheckDateTime()
    {
        var now = timeProvider.GetUtcNow();
        var nextMinute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset).AddMinutes(1);
        var diff = nextMinute - now;
        return diff.TotalSeconds > 30 ? nextMinute : nextMinute.AddMinutes(1);
    }
}
