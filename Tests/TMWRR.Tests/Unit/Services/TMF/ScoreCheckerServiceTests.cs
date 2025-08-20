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
using TMWRR.Services.TMF;

namespace TMWRR.Tests.Unit.Services.TMF;

public class ScoreCheckerServiceTests
{
    private readonly ILogger<ScoreCheckerService> logger;
    private readonly TimeProvider timeProvider;
    private readonly MasterServerTMUF masterServer;
    private readonly IOptionsSnapshot<TMUFOptions> options;
    private readonly IGeneralScoresJobService generalScoresJobService;
    private readonly ICampaignScoresJobService campaignScoresJobService;
    private readonly AppDbContext db;

    public ScoreCheckerServiceTests()
    {
        timeProvider = Substitute.For<TimeProvider>();
        logger = Substitute.For<ILogger<ScoreCheckerService>>();
        masterServer = Substitute.For<MasterServerTMUF>();
        options = Substitute.For<IOptionsSnapshot<TMUFOptions>>();
        generalScoresJobService = Substitute.For<IGeneralScoresJobService>();
        campaignScoresJobService = Substitute.For<ICampaignScoresJobService>();
        db = Substitute.For<AppDbContext>(new DbContextOptionsBuilder<AppDbContext>().Options);
    }

    [Test]
    public async Task ThrowIfOlderThanDay_ShouldReturnDate_WhenDateIsNotOlder()
    {
        // Arrange
        var currentTime = DateTimeOffset.UtcNow;
        timeProvider.GetUtcNow().Returns(currentTime);
        var validDate = currentTime.AddHours(-12); // within one day

        options.Value.Returns(new TMUFOptions
        {
            DiscordWebhookUrl = ""
        });

        var services = new ServiceCollection();
        services.AddResiliencePipeline("scores", x => { });

        var serviceProvider = services.BuildServiceProvider();
        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();

        var scoreCheckerService = new ScoreCheckerService(
            campaignScoresJobService,
            generalScoresJobService,
            masterServer,
            db,
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

        var scoreCheckerService = new ScoreCheckerService(
            campaignScoresJobService,
            generalScoresJobService,
            masterServer,
            db,
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
