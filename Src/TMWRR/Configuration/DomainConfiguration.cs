using TMWRR.Services;
using TMWRR.Services.TMF;

namespace TMWRR.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddHostedService<DailyScoreCheckerHostedService>();
        services.AddScoped<IScoreCheckerService, ScoreCheckerService>();
        services.AddScoped<IGeneralScoresJobService, GeneralScoresJobService>();
        services.AddScoped<ICampaignScoresJobService, CampaignScoresJobService>();

        services.AddSingleton<IDelayService, DelayService>();

        services.AddSingleton(TimeProvider.System);
    }
}
