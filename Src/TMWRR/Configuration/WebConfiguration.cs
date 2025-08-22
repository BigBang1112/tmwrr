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
        services.AddMasterServerTMUF().AddStandardResilienceHandler();

        services.AddResiliencePipeline("scores", x =>
        {
            var options = config.GetSection("TMUF").Get<TMUFOptions>() ?? throw new InvalidOperationException("TMUF options not found");

            x.AddTimeout(options.CheckRetryTimeout)
                .AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<ScoresOlderThanDayException>(),
                    MaxRetryAttempts = int.MaxValue,
                    Delay = options.CheckRetryDelay,
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
