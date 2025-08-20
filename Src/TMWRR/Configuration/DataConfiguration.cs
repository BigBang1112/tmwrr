using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TMWRR.Data;
using TMWRR.Entities;
using TMWRR.Services.TMF;

namespace TMWRR.Configuration;

public static class DataConfiguration
{
    public static void AddDataServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            var connectionStr = config.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuted)) // should be configurable
                .UseSeeding((context, _) =>
                {
                    foreach (var campaignId in ScoreCheckerService.Campaigns)
                    {
                        if (!context.Set<TMUFCampaign>().Any(x => x.Id == campaignId))
                        {
                            context.Set<TMUFCampaign>().Add(new TMUFCampaign { Id = campaignId });
                        }
                    }
                    context.SaveChanges();
                })
                .UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    foreach (var campaignId in ScoreCheckerService.Campaigns)
                    {
                        if (!await context.Set<TMUFCampaign>().AnyAsync(x => x.Id == campaignId, cancellationToken))
                        {
                            await context.Set<TMUFCampaign>().AddAsync(new TMUFCampaign { Id = campaignId }, cancellationToken);
                        }
                    }
                    await context.SaveChangesAsync(cancellationToken);
                });
            //options.UseInMemoryDatabase("TMWRR");
        });
    }

    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
}