using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TmEssentials;
using TMWRR.Data;
using TMWRR.Entities;

namespace TMWRR;

public sealed class Seeding
{
    private const string Resources = "Resources";

    private readonly AppDbContext db;
    private readonly ILogger<Seeding> logger;

    public Seeding(AppDbContext db, ILogger<Seeding> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Games...");

        var noGames = !await db.Games.AnyAsync(cancellationToken);

        if (noGames)
        {
            await using var fs = File.OpenRead(Path.Combine(Resources, "Games.json"));
            var games = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringGameResource, cancellationToken)) ?? [];
            await db.Games.AddRangeAsync(games.Select(x => new Game { Id = x.Key }), cancellationToken);
        }

        logger.LogInformation("Seeding Modes...");

        var noModes = !await db.Modes.AnyAsync(cancellationToken);

        if (noModes)
        {
            await using var fs = File.OpenRead(Path.Combine(Resources, "Modes.json"));
            var modes = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringModeResource, cancellationToken)) ?? [];
            await db.Modes.AddRangeAsync(modes.Select(x => new Mode { Id = x.Key }), cancellationToken);
        }

        logger.LogInformation("Seeding Environments...");

        var environments = await db.Environments.ToListAsync(cancellationToken);

        await using (var fs = File.OpenRead(Path.Combine(Resources, "Environments.json")))
        {
            var environmentDict = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringEnvironmentResource, cancellationToken)) ?? [];

            if (environments.Count == 0)
            {
                await db.Environments.AddRangeAsync(environmentDict.Select(x => new TMEnvironment
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
            }
        }

        logger.LogInformation("Seeding TMFCampaigns...");

        var campaignsTMF = await db.TMFCampaigns.ToListAsync(cancellationToken);

        await using (var fs = File.OpenRead(Path.Combine(Resources, "CampaignsTMF.json")))
        {
            var campaignDict = (await JsonSerializer.DeserializeAsync(fs, AppJsonContext.Default.DictionaryStringCampaignTMFResource, cancellationToken)) ?? [];

            if (campaignsTMF.Count == 0)
            {
                await db.TMFCampaigns.AddRangeAsync(campaignDict.Select(x => new TMFCampaign
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
                    if (campaignDict.TryGetValue(campaign.Id, out var campaignResource))
                    {
                        campaign.Name = campaignResource.Name;
                        campaign.Section = campaignResource.Section;
                        campaign.StartId = campaignResource.StartId;
                    }
                }
            }
        }

        logger.LogInformation("Seeding TMF Maps...");

        var noMaps = !await db.Maps.AnyAsync(cancellationToken);

        await using (var fs = File.OpenRead(Path.Combine(Resources, "MapsTMF.json")))
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

                var loginModel = new TMFLogin { Id = login };
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

                var user = new User { LoginTMF = login };
                userDict[login.Id] = user;
                await db.Users.AddAsync(user, cancellationToken);
            }

            if (noMaps)
            {
                await db.Maps.AddRangeAsync(mapDict.Select(x => new Map
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
                    Thumbnail = x.Value.Thumbnail
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
                    map.Thumbnail = mapResource.Thumbnail;
                }

                var existingMapUids = maps.Select(x => x.MapUid).ToHashSet();
                var missingMaps = mapDict
                    .Where(kv => !existingMapUids.Contains(kv.Key))
                    .Select(kv => new Map
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
                        Thumbnail = kv.Value.Thumbnail
                    });

                await db.Maps.AddRangeAsync(missingMaps, cancellationToken);
            }
        }

        logger.LogInformation("Saving seeding...");

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeding completed successfully.");
    }
}
