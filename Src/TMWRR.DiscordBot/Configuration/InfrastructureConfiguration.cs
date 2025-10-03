using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TMWRR.DiscordBot.Options;
using TMWRR.DiscordBot.Services;

namespace TMWRR.DiscordBot.Configuration
{
    public static class InfrastructureConfiguration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<IGitHubService, GitHubService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TMWRR.DiscordBot/1.0 (+https://example.com; support@example.com)");
            }).AddStandardResilienceHandler();

            services.AddSingleton(TimeProvider.System);

            services.AddOptions<DiscordOptions>()
                .Bind(config.GetSection(DiscordOptions.Discord))
                .ValidateDataAnnotations();
        }
    }
}
