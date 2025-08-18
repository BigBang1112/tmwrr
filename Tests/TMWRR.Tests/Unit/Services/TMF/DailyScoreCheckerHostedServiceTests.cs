using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TMWRR.Options;
using TMWRR.Services.TMF;

namespace TMWRR.Tests.Unit.Services.TMF;

public class DailyScoreCheckerHostedServiceTests
{
    private readonly ILogger<DailyScoreCheckerHostedService> logger;
    private readonly TimeProvider timeProvider;
    private readonly IOptions<TMUFOptions> options;

    public DailyScoreCheckerHostedServiceTests()
    {
        timeProvider = Substitute.For<TimeProvider>();
        options = Substitute.For<IOptions<TMUFOptions>>();
        logger = Substitute.For<ILogger<DailyScoreCheckerHostedService>>();
    }

    [Test]
    public async Task GetNextCheckDateTime_CEST_BeforeCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 4, 14, 8, 0, 0, TimeSpan.Zero); // 14 April 2025 8:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(2)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_CEST_AfterCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 4, 14, 12, 0, 0, TimeSpan.Zero); // 14 April 2025 12:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST.Add(TimeSpan.FromDays(1)) - TimeSpan.FromHours(2)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_CET_BeforeCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 11, 14, 8, 0, 0, TimeSpan.Zero); // 14 November 2025 8:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(1)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_CET_AfterCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 11, 14, 12, 0, 0, TimeSpan.Zero); // 14 November 2025 12:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST.Add(TimeSpan.FromDays(1)) - TimeSpan.FromHours(1)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task RunScoreCheckAsync_ShouldReturnExpectedScoresResult()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 14, 8, 0, 0, TimeSpan.Zero); // 14 November 2025 8:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST
        });

        var expectedNextCheck = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(1)), TimeSpan.Zero);
        var expectedNextNumber = ScoresNumber.Scores6;

        var scoreCheckerService = Substitute.For<IScoreCheckerService>();
        scoreCheckerService.CheckScoresAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedNextNumber));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => scoreCheckerService);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var service = new DailyScoreCheckerHostedService(serviceScopeFactory, timeProvider, options, logger);

        // Setup GetNextCheckDateTime
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.RunScoreCheckAsync(null, cancellationToken);

        // Assert
        await Assert.That(result.NextNumber).IsNotNull();
        await Assert.That(expectedNextCheck).IsEqualTo(result.NextCheckAt);
        await Assert.That(expectedNextNumber).IsEqualTo(result.NextNumber!.Value);

        // Verify scoreCheckerService was called
        await scoreCheckerService.Received(1).CheckScoresAsync(null, cancellationToken);
    }
}
