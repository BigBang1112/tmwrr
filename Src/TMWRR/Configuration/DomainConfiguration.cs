﻿using TMWRR.Services.TMF;

namespace TMWRR.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IScoreCheckerService, ScoreCheckerService>();
        services.AddHostedService<DailyScoreCheckerHostedService>();

        services.AddSingleton(TimeProvider.System);
    }
}
