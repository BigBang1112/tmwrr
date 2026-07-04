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
            Discord = new TMUFDiscord { TestWebhookUrl = "" },
            WebServices = new TMUFWebServices { ApiUsername = "", ApiPassword = "" }
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
            Discord = new TMUFDiscord { TestWebhookUrl = "" },
            WebServices = new TMUFWebServices { ApiUsername = "", ApiPassword = "" }
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
            Discord = new TMUFDiscord { TestWebhookUrl = "" },
            WebServices = new TMUFWebServices { ApiUsername = "", ApiPassword = "" }
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
            Discord = new TMUFDiscord { TestWebhookUrl = "" },
            WebServices = new TMUFWebServices { ApiUsername = "", ApiPassword = "" }
        });

        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST.Add(TimeSpan.FromDays(1)) - TimeSpan.FromHours(1)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, delayService, timeProvider, options, logger);

        // Act
        var nextCheckDateTime = service.GetNextCheckDateTime();

        // Assert
        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task GetNextCheckDateTime_DaylightSavingTransition_PreDST_ReturnsCorrectNextDateTime()
    {
        // 29. 3. 2025 (sobota) – stále CET (UTC+1)
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => options);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var now = new DateTimeOffset(2025, 3, 29, 8, 0, 0, TimeSpan.Zero); // 08:00 UTC
        timeProvider.GetUtcNow().Returns(now);

        var checkTimeOfDayCEST = new TimeSpan(11, 0, 0); // 11:00 "CEST" definovaný v konfiguraci
        options.Value.Returns(new TMUFOptions
        {
            CheckTimeOfDayCEST = checkTimeOfDayCEST,
            Discord = new TMUFDiscord { TestWebhookUrl = "" },
            WebServices = new TMUFWebServices { ApiUsername = "", ApiPassword = "" }
        });

        // Očekáváme offset -1 hod (CET) => 10:00 UTC
        var expectedTime = new DateTimeOffset(now.Date.Add(checkTimeOfDayCEST - TimeSpan.FromHours(1)), TimeSpan.Zero);

        var service = new DailyScoreCheckerHostedService(scopeFactory, delayService, timeProvider, options, logger);

        var nextCheckDateTime = service.GetNextCheckDateTime();

        await Assert.That(nextCheckDateTime).IsEqualTo(expectedTime);
    }
}
