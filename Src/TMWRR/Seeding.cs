using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.Text.Json;
using TmEssentials;
using TMWRR.Data;
using TMWRR.Entities;
using TMWRR.Entities.TMF;
using TMWRR.Options;

namespace TMWRR;

public sealed class Seeding
{
    private const string Resources = "Resources";
    private const string Thumbnails = "Thumbnails";

    private readonly AppDbContext db;
    private readonly HybridCache hybridCache;
    private readonly IOutputCacheStore outputCache;
    private readonly IFileSystem fileSystem;
    private readonly IOptions<DatabaseOptions> options;
    private readonly ILogger<Seeding> logger;

    public Seeding(
        AppDbContext db, 
        HybridCache hybridCache, 
        IOutputCacheStore outputCache, 
        IFileSystem fileSystem, 
        IOptions<DatabaseOptions> options,
        ILogger<Seeding> logger)
    {
        this.db = db;
        this.hybridCache = hybridCache;
        this.outputCache = outputCache;
        this.fileSystem = fileSystem;
        this.options = options;
        this.logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.EnableSeeding)
        {
            logger.LogInformation("Skipping seeding.");
            return;
        }

        logger.LogInformation("Seeding Games...");

        var noGames = !await db.Games.AnyAsync(cancellationToken);

        if (noGames)
        {
            await using var fs = fileSystem.File.OpenRead(fileSystem.Path.Combine(Resources, "Games.json"));
            var games = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringGameResource, cancellationToken)) ?? [];
            await db.Games.AddRangeAsync(games.Select(x => new GameEntity { Id = x.Key }), cancellationToken);
        }

        logger.LogInformation("Seeding Modes...");

        var noModes = !await db.Modes.AnyAsync(cancellationToken);

        if (noModes)
        {
            await using var fs = fileSystem.File.OpenRead(fileSystem.Path.Combine(Resources, "Modes.json"));
            var modes = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringModeResource, cancellationToken)) ?? [];
            await db.Modes.AddRangeAsync(modes.Select(x => new ModeEntity { Id = x.Key }), cancellationToken);
        }

        logger.LogInformation("Seeding Environments...");

        var environments = await db.Environments.ToListAsync(cancellationToken);

        await using (var fs = fileSystem.File.OpenRead(fileSystem.Path.Combine(Resources, "Environments.json")))
        {
            var environmentDict = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringEnvironmentResource, cancellationToken)) ?? [];

            if (environments.Count == 0)
            {
                await db.Environments.AddRangeAsync(environmentDict.Select(x => new TMEnvironmentEntity
                {
                    Id = x.Key,
                    Name = x.Value.Name,
                    GameId = x.Value.Game
                }), cancellationToken);
            }
            else
            {
                foreach (var environment in environments)
                {
                    if (environmentDict.TryGetValue(environment.Id, out var environmentResource))
                    {
                        environment.Name = environmentResource.Name;
                        environment.GameId = environmentResource.Game;
                    }
                }

                var existingEnvironmentIds = environments.Select(x => x.Id).ToHashSet();
                var missingEnvironments = environmentDict
                    .Where(kv => !existingEnvironmentIds.Contains(kv.Key))
                    .Select(kv => new TMEnvironmentEntity
                    {
                        Id = kv.Key,
                        Name = kv.Value.Name,
                        GameId = kv.Value.Game
                    });
                await db.Environments.AddRangeAsync(missingEnvironments, cancellationToken);
            }
        }

        logger.LogInformation("Seeding TMFCampaigns...");

        var campaignsTMF = await db.TMFCampaigns.ToListAsync(cancellationToken);

        await using (var fs = fileSystem.File.OpenRead(fileSystem.Path.Combine(Resources, "CampaignsTMF.json")))
        {
            var campaignDict = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringCampaignTMFResource, cancellationToken)) ?? [];

            if (campaignsTMF.Count == 0)
            {
                await db.TMFCampaigns.AddRangeAsync(campaignDict.Select(x => new TMFCampaignEntity
                {
                    Id = x.Key,
                    Name = x.Value.Name,
                    Section = x.Value.Section,
                    StartId = x.Value.StartId,
                }), cancellationToken);
            }
            else
            {
                foreach (var campaign in campaignsTMF)
                {
                    if (!campaignDict.TryGetValue(campaign.Id, out var campaignResource))
                    {
                        continue;
                    }

                    campaign.Name = campaignResource.Name;
                    campaign.Section = campaignResource.Section;
                    campaign.StartId = campaignResource.StartId;
                }
            }
        }

        logger.LogInformation("Seeding TMF Maps...");

        var noMaps = !await db.Maps.AnyAsync(cancellationToken);

        await using (var fs = fileSystem.File.OpenRead(fileSystem.Path.Combine(Resources, "MapsTMF.json")))
        {
            var mapDict = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringMapTMFResource, cancellationToken)) ?? [];

            var logins = mapDict.Values
                .Select(x => x.AuthorLogin)
                .Distinct()
                .ToList();

            var loginDict = await db.TMFLogins
                .Where(x => logins.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            foreach (var login in logins)
            {
                if (loginDict.ContainsKey(login))
                {
                    continue;
                }

                var loginModel = new TMFLoginEntity { Id = login };
                loginDict[login] = loginModel;
                await db.TMFLogins.AddAsync(loginModel, cancellationToken);
            }

            var userDict = await db.Users
                .Where(x => x.LoginTMF != null && logins.Contains(x.LoginTMF.Id))
                .ToDictionaryAsync(x => x.LoginTMF!.Id, cancellationToken);

            foreach (var login in loginDict.Values)
            {
                if (userDict.ContainsKey(login.Id))
                {
                    continue;
                }

                var user = new UserEntity { LoginTMF = login };
                userDict[login.Id] = user;
                await db.Users.AddAsync(user, cancellationToken);
            }

            if (noMaps)
            {
                await db.Maps.AddRangeAsync(mapDict.Select(x => new MapEntity
                {
                    MapUid = x.Key,
                    Name = x.Value.Name,
                    DeformattedName = TextFormatter.Deformat(x.Value.Name),
                    Author = userDict.GetValueOrDefault(x.Value.AuthorLogin),
                    EnvironmentId = x.Value.Environment,
                    ModeId = x.Value.Mode,
                    AuthorTime = x.Value.AuthorTime is null ? null : new TimeInt32(x.Value.AuthorTime.Value),
                    AuthorScore = x.Value.AuthorScore,
                    NbLaps = x.Value.NbLaps,
                    TMFCampaignId = x.Value.Campaign,
                    Order = x.Value.Order,
                    FileName = x.Value.FileName,
                    Thumbnail = fileSystem.File.Exists(fileSystem.Path.Combine(Resources, Thumbnails, x.Key + ".jpg"))
                        ? fileSystem.File.ReadAllBytes(fileSystem.Path.Combine(Resources, Thumbnails, x.Key + ".jpg"))
                        : null
                }), cancellationToken);
            }
            else
            {
                var maps = await db.Maps
                    .Where(x => mapDict.Keys.Contains(x.MapUid))
                    .ToListAsync(cancellationToken);

                foreach (var map in maps)
                {
                    if (!mapDict.TryGetValue(map.MapUid, out var mapResource))
                    {
                        continue;
                    }

                    map.Name = mapResource.Name;
                    map.DeformattedName = TextFormatter.Deformat(mapResource.Name);
                    map.Author = userDict.GetValueOrDefault(mapResource.AuthorLogin);
                    map.EnvironmentId = mapResource.Environment;
                    map.ModeId = mapResource.Mode;
                    map.AuthorTime = mapResource.AuthorTime is null ? null : new TimeInt32(mapResource.AuthorTime.Value);
                    map.AuthorScore = mapResource.AuthorScore;
                    map.NbLaps = mapResource.NbLaps;
                    map.TMFCampaignId = mapResource.Campaign;
                    map.Order = mapResource.Order;
                    map.FileName = mapResource.FileName;
                    map.Thumbnail = fileSystem.File.Exists(fileSystem.Path.Combine(Resources, Thumbnails, map.MapUid + ".jpg"))
                        ? fileSystem.File.ReadAllBytes(fileSystem.Path.Combine(Resources, Thumbnails, map.MapUid + ".jpg"))
                        : null;
                }

                var existingMapUids = maps.Select(x => x.MapUid).ToHashSet();
                var missingMaps = mapDict
                    .Where(kv => !existingMapUids.Contains(kv.Key))
                    .Select(kv => new MapEntity
                    {
                        MapUid = kv.Key,
                        Name = kv.Value.Name,
                        DeformattedName = TextFormatter.Deformat(kv.Value.Name),
                        Author = userDict.GetValueOrDefault(kv.Value.AuthorLogin),
                        EnvironmentId = kv.Value.Environment,
                        ModeId = kv.Value.Mode,
                        AuthorTime = kv.Value.AuthorTime is null ? null : new TimeInt32(kv.Value.AuthorTime.Value),
                        AuthorScore = kv.Value.AuthorScore,
                        NbLaps = kv.Value.NbLaps,
                        TMFCampaignId = kv.Value.Campaign,
                        Order = kv.Value.Order,
                        FileName = kv.Value.FileName,
                        Thumbnail = fileSystem.File.Exists(fileSystem.Path.Combine(Resources, Thumbnails, kv.Key + ".jpg"))
                            ? fileSystem.File.ReadAllBytes(fileSystem.Path.Combine(Resources, Thumbnails, kv.Key + ".jpg"))
                            : null
                    });

                await db.Maps.AddRangeAsync(missingMaps, cancellationToken);
            }
        }

        logger.LogInformation("Saving seeding...");

        await db.SaveChangesAsync(cancellationToken);

        await hybridCache.RemoveByTagAsync("map", cancellationToken);
        await outputCache.EvictByTagAsync("game", cancellationToken);
        await outputCache.EvictByTagAsync("environment", cancellationToken);
        await outputCache.EvictByTagAsync("mode", cancellationToken);
        await outputCache.EvictByTagAsync("campaign", cancellationToken);
        await outputCache.EvictByTagAsync("map", cancellationToken);

        logger.LogInformation("Seeding completed successfully.");
    }
}
