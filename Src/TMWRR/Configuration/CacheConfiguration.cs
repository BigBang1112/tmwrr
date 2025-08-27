namespace TMWRR.Configuration;

public static class CacheConfiguration
{
    public static void AddCacheServices(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddPolicy(CachePolicy.Games, x =>
                x.Tag("game", "environment").Expire(TimeSpan.FromDays(1)));

            options.AddPolicy(CachePolicy.Environments, x =>
                x.Tag("environment", "game").Expire(TimeSpan.FromDays(1)));

            options.AddPolicy(CachePolicy.Campaigns, x =>
                x.Tag("campaign").Expire(TimeSpan.FromDays(1)));
        });

        services.AddHybridCache();
    }

    public static void UseCacheMiddleware(this WebApplication app)
    {
        app.UseOutputCache();
    }
}
