using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TMWRR.Api;
using TMWRR.DiscordBot.Options;
using TMWRR.DiscordBot.Services;

namespace TMWRR.DiscordBot.Configuration;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<DiscordOptions>()
            .Bind(config.GetSection(DiscordOptions.Discord))
            .ValidateDataAnnotations();

        services.AddOptions<ApiOptions>()
            .Bind(config.GetSection(ApiOptions.API))
            .ValidateDataAnnotations();

        services.AddHttpClient()
            .ConfigureHttpClientDefaults(httpBuilder =>
            {
                httpBuilder.ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(config["API:BaseAddress"] ?? throw new InvalidOperationException("API:BaseAddress configuration is missing"));
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("TMWRR.DiscordBot/0.1 (TM World Record Report Discord Bot; Discord=bigbang1112)");
                });
            });

        services.AddScoped(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            return new TmwrrClient(httpClient);
        });
    }
}
