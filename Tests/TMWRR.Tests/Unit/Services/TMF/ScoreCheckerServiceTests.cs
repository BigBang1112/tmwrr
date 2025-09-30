using ManiaAPI.Xml.TMUF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Polly;
using Polly.Registry;
using TMWRR.Data;
using TMWRR.Exceptions;
using TMWRR.Options;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Tests.Unit.Services.TMF;

public class ScoreCheckerServiceTests
{
    private readonly ILogger<ScoresCheckerService> logger = Substitute.For<ILogger<ScoresCheckerService>>();
    private readonly TimeProvider timeProvider = Substitute.For<TimeProvider>();
    private readonly MasterServerTMUF masterServer = Substitute.For<MasterServerTMUF>();
    private readonly IOptionsSnapshot<TMUFOptions> options = Substitute.For<IOptionsSnapshot<TMUFOptions>>();
    private readonly IGeneralScoresJobService generalScoresJobService = Substitute.For<IGeneralScoresJobService>();
    private readonly ICampaignScoresJobService campaignScoresJobService = Substitute.For<ICampaignScoresJobService>();
    private readonly ILadderScoresJobService ladderScoresJobService = Substitute.For<ILadderScoresJobService>();
    private readonly IScoresSnapshotService scoresSnapshotService = Substitute.For<IScoresSnapshotService>();
    private readonly IReportService reportService = Substitute.For<IReportService>();

    [Test]
    public async Task ThrowIfOlderThanDay_ShouldReturnDate_WhenDateIsNotOlder()
    {
        // Arrange
        var currentTime = DateTimeOffset.UtcNow;
        timeProvider.GetUtcNow().Returns(currentTime);
        var validDate = currentTime.AddHours(-12); // within one day

        options.Value.Returns(new TMUFOptions
        {
            Discord = new TMUFDiscord
            {
                TestWebhookUrl = ""
            },
            WebServices = new TMUFWebServices
            {
                ApiUsername = "",
                ApiPassword = ""
            }
        });

        var services = new ServiceCollection();
        services.AddResiliencePipeline("scores", x => { });

        var serviceProvider = services.BuildServiceProvider();
        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();

        var scoreCheckerService = new ScoresCheckerService(
            campaignScoresJobService,
            generalScoresJobService,
            ladderScoresJobService,
            scoresSnapshotService,
            reportService,
            masterServer,
            timeProvider,
            pipelineProvider,
            options,
            logger);

        // Act
        var result = scoreCheckerService.ThrowIfOlderThanDay(validDate);

        // Assert
        await Assert.That(result).IsEqualTo(validDate);
    }

    [Test]
    public void ThrowIfOlderThanDay_ShouldThrow_WhenDateIsOlderThanOneDay()
    {
        // Arrange
        var currentTime = DateTimeOffset.UtcNow;
        timeProvider.GetUtcNow().Returns(currentTime);
        var oldDate = currentTime.AddDays(-2); // older than one day

        var services = new ServiceCollection();
        services.AddResiliencePipeline("scores", x => { });

        var serviceProvider = services.BuildServiceProvider();
        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();

        var scoreCheckerService = new ScoresCheckerService(
            campaignScoresJobService,
            generalScoresJobService,
            ladderScoresJobService,
            scoresSnapshotService,
            reportService,
            masterServer,
            timeProvider,
            pipelineProvider,
            options,
            logger);

        // Act & Assert
        Assert.Throws<ScoresOlderThanDayException>(() =>
        {
            scoreCheckerService.ThrowIfOlderThanDay(oldDate);
        });
    }
}
