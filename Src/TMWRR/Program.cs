using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using Scalar.AspNetCore;
using HealthChecks.UI.Client;
using TMWRR.Services.TMF;
using ManiaAPI.Xml.Extensions.Hosting;
using TMWRR.Options;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Add services to the container.
builder.Services.AddMasterServerTMUF().AddStandardResilienceHandler();
builder.Services.AddScoped<IScoreCheckerService, ScoreCheckerService>();
builder.Services.AddHostedService<DailyScoreCheckerHostedService>();

builder.Services.Configure<TMUFOptions>(builder.Configuration.GetSection("TMUF"));

builder.Services.AddResiliencePipeline("scores", x =>
{
    var options = builder.Configuration.GetSection("TMUF").Get<TMUFOptions>() ?? throw new InvalidOperationException("TMUF options not found");

    x.AddTimeout(options.CheckRetryTimeout)
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(), // temp
            MaxRetryAttempts = int.MaxValue,
            Delay = options.CheckRetryDelay,
            UseJitter = true,
        })
        .Build();
});

builder.Services.AddOpenApi();

builder.Services.AddOutputCache();
builder.Services.AddHybridCache();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
});

builder.Services.AddHealthChecks();

builder.Services.AddSingleton(TimeProvider.System);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, applyThemeToRedirectedOutput: true)
    .WriteTo.OpenTelemetry()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddOpenTelemetry()
    .WithMetrics(options =>
    {
        options
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter();

        options.AddMeter("System.Net.Http");
    })
    .WithTracing(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.SetSampler<AlwaysOnSampler>();
        }

        options
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter();
    });
builder.Services.AddMetrics();

var app = builder.Build();

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

app.UseOutputCache();

app.Run();

public partial class Program;