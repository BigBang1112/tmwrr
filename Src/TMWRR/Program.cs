using TMWRR.Options;
using TMWRR.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.Configure<TMUFOptions>(builder.Configuration.GetSection("TMUF"));

// Add services to the container.
builder.Services.AddDomainServices();
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseMiddleware();

app.Run();

public partial class Program;