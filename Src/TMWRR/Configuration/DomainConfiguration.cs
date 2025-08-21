using TMWRR.DiscordReport;
using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<Seeding>();

        services.AddHostedService<DailyScoreCheckerHostedService>();
        services.AddScoped<IScoresCheckerService, ScoresCheckerService>();
        services.AddScoped<IGeneralScoresJobService, GeneralScoresJobService>();
        services.AddScoped<ICampaignScoresJobService, CampaignScoresJobService>();
        services.AddScoped<ILadderScoresJobService, LadderScoresJobService>();
        services.AddScoped<IScoresSnapshotService, ScoresSnapshotService>();

        services.AddScoped<IMapService, MapService>();
        services.AddScoped<ILoginService, LoginService>();

        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportDiscordService, ReportDiscordService>();
        services.AddSingleton<IDiscordWebhookFactory, DiscordWebhookFactory>();

        services.AddSingleton<IDelayService, DelayService>();

        services.AddSingleton(TimeProvider.System);
    }
}
