using ManiaAPI.Xml.TMUF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TMWRR.Options;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Tests.Unit.Services.TMF;

public class DailyScoreCheckerHostedServiceTests
{
    private readonly ILogger<DailyScoreCheckerHostedService> logger;
    private readonly TimeProvider timeProvider;
    private readonly IOptionsSnapshot<TMUFOptions> options;
    private readonly IDelayService delayService;

    public DailyScoreCheckerHostedServiceTests()
    {
        timeProvider = Substitute.For<TimeProvider>();
        options = Substitute.For<IOptionsSnapshot<TMUFOptions>>();
        logger = Substitute.For<ILogger<DailyScoreCheckerHostedService>>();
        delayService = Substitute.For<IDelayService>();
    }

    [Test]
    public async Task GetNextCheckDateTime_CEST_BeforeCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => options);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 4, 14, 8, 0, 0, TimeSpan.Zero); // 14 April 2025 8:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST,
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(2)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, delayService, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_CEST_AfterCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => options);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 4, 14, 12, 0, 0, TimeSpan.Zero); // 14 April 2025 12:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST,
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST.Add(TimeSpan.FromDays(1)) - TimeSpan.FromHours(2)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, delayService, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_CET_BeforeCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => options);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 11, 14, 8, 0, 0, TimeSpan.Zero); // 14 November 2025 8:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST,
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(1)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, delayService, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_CET_AfterCheckHour_ReturnsCorrectNextDateTime()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => options);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 11, 14, 12, 0, 0, TimeSpan.Zero); // 14 November 2025 12:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 CEST
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST,
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST.Add(TimeSpan.FromDays(1)) - TimeSpan.FromHours(1)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, delayService, timeProvider, options, logger);

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
            CheckTimeOfDayCEST = checkTimeOfDayCEST,
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        var expectedNextCheck = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(1)), TimeSpan.Zero);
        var expectedNextNumber = new ScoresNumber?(ScoresNumber.Scores6);

        var scoreCheckerService = Substitute.For<IScoresCheckerService>();
        scoreCheckerService.CheckScoresAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedNextNumber));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => scoreCheckerService);
        serviceCollection.AddScoped(_ => options);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var service = new DailyScoreCheckerHostedService(serviceScopeFactory, delayService, timeProvider, options, logger);

        // Setup GetNextCheckDateTime
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.RunScoreCheckAsync(null, cancellationToken);

        // Assert
        await Assert.That(result.NextNumber).IsEqualTo(expectedNextNumber);
        await Assert.That(result.NextCheckAt).IsEqualTo(expectedNextCheck);

        // Verify scoreCheckerService was called
        await scoreCheckerService.Received(1).CheckScoresAsync(null, cancellationToken);
    }

    [Test]
    public async Task WaitForNextCheckAsync_FutureDelay_SuccessfulScoreCheck_ReturnsScoreResult()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 04, 14, 8, 0, 0, TimeSpan.Zero);
        timeProvider.GetUtcNow().Returns(now);
        var delayTime = TimeSpan.FromMinutes(1);
        var nextCheckAt = now.Add(delayTime);
        var expectedNumber = new ScoresNumber?(ScoresNumber.Scores6);

        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = new TimeSpan(11, 0, 0),
            CheckRetryDelay = TimeSpan.FromSeconds(5),
            CheckRetryTimeout = TimeSpan.FromSeconds(30),
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        // Setup delay to complete successfully.
        delayService.DelayAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var scoreCheckerService = Substitute.For<IScoresCheckerService>();

        // Setup the scoreCheckerService to return a valid result.
        // The RunScoreCheckAsync method uses the result of CheckScoresAsync.
        scoreCheckerService.CheckScoresAsync(Arg.Any<ScoresNumber?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedNumber));

        var services = new ServiceCollection();
        services.AddScoped(_ => scoreCheckerService);
        services.AddScoped(_ => options);

        var serviceProvider = services.BuildServiceProvider();
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var cancellationToken = CancellationToken.None;

        var service = new DailyScoreCheckerHostedService(serviceScopeFactory, delayService, timeProvider, options, logger);

        // Act
        var result = await service.WaitForNextCheckAsync(nextCheckAt, expectedNumber, cancellationToken);

        // Assert
        // We expect the result to equal whatever RunScoreCheckAsync returns.
        // RunScoreCheckAsync internally builds the result as:
        // new ScoresResult(GetNextCheckDateTime(), scoreCheckerService.CheckScoresAsync(...))
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Value.NextNumber).IsEqualTo(expectedNumber);

        // Verify that delay was called with roughly the expected delay (allowing small time differences)
        await delayService.Received(1).DelayAsync(Arg.Is<TimeSpan>(d => d.TotalSeconds >= delayTime.TotalSeconds - 0.1 &&
                                                                            d.TotalSeconds <= delayTime.TotalSeconds + 0.1), cancellationToken);
        // Verify that score checking call was made.
        await scoreCheckerService.Received(1).CheckScoresAsync(expectedNumber, cancellationToken);
    }

    [Test]
    public async Task WaitForNextCheckAsync_FutureDelay_TaskCanceled_ReturnsNull()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 04, 14, 8, 0, 0, TimeSpan.Zero);
        timeProvider.GetUtcNow().Returns(now);
        var delayTime = TimeSpan.FromMinutes(1);
        var nextCheckAt = now.Add(delayTime);
        var expectedNumber = ScoresNumber.Scores6;

        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = new TimeSpan(11, 0, 0),
            CheckRetryDelay = TimeSpan.FromSeconds(5),
            CheckRetryTimeout = TimeSpan.FromSeconds(30),
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        // Setup delay to throw TaskCanceledException.
        delayService.DelayAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Throws(new TaskCanceledException());

        var scoreCheckerService = Substitute.For<IScoresCheckerService>();

        var services = new ServiceCollection();
        services.AddScoped(_ => scoreCheckerService);
        services.AddScoped(_ => options);

        var serviceProvider = services.BuildServiceProvider();
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var cancellationToken = CancellationToken.None;
        var service = new DailyScoreCheckerHostedService(serviceScopeFactory, delayService, timeProvider, options, logger);

        // Act
        var result = await service.WaitForNextCheckAsync(nextCheckAt, expectedNumber, cancellationToken);

        // Assert
        await Assert.That(result).IsNull();

        // Verify delay was attempted.
        await delayService.Received(1).DelayAsync(Arg.Any<TimeSpan>(), cancellationToken);
        // Verify that score checking call was never made.
        await scoreCheckerService.DidNotReceiveWithAnyArgs().CheckScoresAsync(default, default);
    }

    [Test]
    public async Task WaitForNextCheckAsync_PastDelay_SuccessfulScoreCheck_ReturnsScoreResult()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 04, 14, 8, 0, 0, TimeSpan.Zero);
        timeProvider.GetUtcNow().Returns(now);
        var pastDelay = TimeSpan.FromMinutes(-1);
        var nextCheckAt = now.Add(pastDelay);
        var expectedNumber = new ScoresNumber?(ScoresNumber.Scores4);

        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = new TimeSpan(11, 0, 0),
            CheckRetryDelay = TimeSpan.FromSeconds(5),
            CheckRetryTimeout = TimeSpan.FromSeconds(30),
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        var scoreCheckerService = Substitute.For<IScoresCheckerService>();

        // For past delays, delayService.DelayAsync should never be called.
        scoreCheckerService.CheckScoresAsync(Arg.Any<ScoresNumber?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedNumber));

        var services = new ServiceCollection();
        services.AddScoped(_ => scoreCheckerService);
        services.AddScoped(_ => options);

        var serviceProvider = services.BuildServiceProvider();
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var cancellationToken = CancellationToken.None;
        var service = new DailyScoreCheckerHostedService(serviceScopeFactory, delayService, timeProvider, options, logger);

        // Act
        var result = await service.WaitForNextCheckAsync(nextCheckAt, expectedNumber, cancellationToken);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Value.NextNumber).IsEqualTo(expectedNumber);

        // Verify that delay service was not called.
        await delayService.DidNotReceiveWithAnyArgs().DelayAsync(default, default);
        await scoreCheckerService.Received(1).CheckScoresAsync(expectedNumber, cancellationToken);
    }

    [Test]
    public async Task WaitForNextCheckAsync_RunScoreCheckThrows_ReturnsFallbackScoreResult()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 14, 8, 0, 0, TimeSpan.Zero);
        timeProvider.GetUtcNow().Returns(now);
        var delayTime = TimeSpan.FromMinutes(1);
        var nextCheckAt = now.Add(delayTime);
        var expectedNumber = ScoresNumber.Scores7; // This value will not be returned because of exception

        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = new TimeSpan(11, 0, 0),
            CheckRetryDelay = TimeSpan.FromSeconds(5),
            CheckRetryTimeout = TimeSpan.FromSeconds(30),
            DiscordWebhookUrl = "",
            ChangesDiscordWebhookUrl = "",
            ApiUsername = "",
            ApiPassword = ""
        });

        // Setup delay to complete successfully.
        delayService.DelayAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var scoreCheckerService = Substitute.For<IScoresCheckerService>();

        var services = new ServiceCollection();
        services.AddScoped(_ => scoreCheckerService);
        services.AddScoped(_ => options);

        var serviceProvider = services.BuildServiceProvider();
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // Setup the scoreCheckerService to throw an exception when called.
        scoreCheckerService.CheckScoresAsync(Arg.Any<ScoresNumber?>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Test exception"));

        var cancellationToken = CancellationToken.None;
        var service = new DailyScoreCheckerHostedService(serviceScopeFactory, delayService, timeProvider, options, logger);

        // Act
        var result = await service.WaitForNextCheckAsync(nextCheckAt, expectedNumber, cancellationToken);

        // Assert
        // In case of exception in RunScoreCheckAsync, the fallback is to return:
        // new ScoresResult(GetNextCheckDateTime(), NextNumber: null);
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Value.NextNumber).IsNull();
        var expectedNextCheckAt = service.GetNextCheckDateTime();
        await Assert.That(result.Value.NextCheckAt).IsEqualTo(expectedNextCheckAt);

        // Verify that delay was called.
        await delayService.Received(1).DelayAsync(Arg.Any<TimeSpan>(), cancellationToken);
        // Verify that score checking was attempted.
        await scoreCheckerService.Received(1).CheckScoresAsync(expectedNumber, cancellationToken);
    }
}
