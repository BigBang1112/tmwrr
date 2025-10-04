using ManiaAPI.TrackmaniaWS;
using ManiaAPI.TrackmaniaWS.Extensions.Hosting;
using ManiaAPI.UnitedLadder;
using ManiaAPI.Xml.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using System.Text.Json.Serialization;
using TMWRR.Api;
using TMWRR.Api.Converters.Json;
using TMWRR.Exceptions;
using TMWRR.Options;
using TMWRR.Services;

namespace TMWRR.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        var tmufOptions = config.GetSection("TMUF").Get<TMUFOptions>() ?? throw new InvalidOperationException("TMUF options not found");

        services.AddHttpClient<IGhostService, GhostService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TMWRR/0.1 (World Record Report v3; Email=petrpiv1@gmail.com; Discord=bigbang1112)");
        }).AddStandardResilienceHandler();

        services.AddMasterServerTMUF().AddStandardResilienceHandler();
        services.AddTrackmaniaWS(new TrackmaniaWSOptions
        {
            Credentials = new TrackmaniaWSCredentials(tmufOptions.WebServices.ApiUsername, tmufOptions.WebServices.ApiPassword)
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

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, TmwrrJsonSerializerContext.Default);
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.Converters.Add(new JsonTimeInt32Converter());
        });

        services.AddHealthChecks();

        services.AddProblemDetails();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
}
