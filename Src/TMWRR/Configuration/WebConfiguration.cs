using HealthChecks.UI.Client;
using ManiaAPI.Xml.Extensions.Hosting;
using Polly;
using Scalar.AspNetCore;
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

        services.AddHealthChecks();
    }

    public static void UseSecurityMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Theme = ScalarTheme.DeepSpace;
        });

        app.UseRateLimiter();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();
    }
}
