using TMWRR.Options;
using TMWRR.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddOptions<TMUFOptions>()
    .Bind(builder.Configuration.GetSection("TMUF"))
    .ValidateDataAnnotations();

// Add services to the container.
builder.Services.AddDomainServices();
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.MigrateDatabase();
}

app.UseMiddleware();

app.Run();

public partial class Program;