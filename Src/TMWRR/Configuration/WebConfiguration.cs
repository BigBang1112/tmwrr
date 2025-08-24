using ManiaAPI.TrackmaniaWS;
using ManiaAPI.TrackmaniaWS.Extensions.Hosting;
using ManiaAPI.UnitedLadder;
using ManiaAPI.Xml.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Polly;
using System.Text.Json.Serialization;
using TMWRR.Exceptions;
using TMWRR.Options;

namespace TMWRR.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        var tmufOptions = config.GetSection("TMUF").Get<TMUFOptions>() ?? throw new InvalidOperationException("TMUF options not found");

        services.AddMasterServerTMUF().AddStandardResilienceHandler();
        services.AddTrackmaniaWS(new TrackmaniaWSOptions
        {
            Credentials = new TrackmaniaWSCredentials(tmufOptions.ApiUsername, tmufOptions.ApiPassword)
        }).AddStandardResilienceHandler();
        services.AddHttpClient<UnitedLadder>().AddStandardResilienceHandler();

        services.AddResiliencePipeline("scores", x =>
        {
            x.AddTimeout(tmufOptions.CheckRetryTimeout)
                .AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<ScoresOlderThanDayException>(),
                    MaxRetryAttempts = int.MaxValue,
                    Delay = tmufOptions.CheckRetryDelay,
                    UseJitter = true,
                })
                .Build();
        });

        services.AddOpenApi();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
        });

        services.Configure<JsonOptions>(options =>
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        services.AddHealthChecks();
    }
}
