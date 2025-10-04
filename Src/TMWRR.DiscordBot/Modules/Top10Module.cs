using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using TmEssentials;
using TMWRR.Api;
using TMWRR.Api.TMF;
using TMWRR.DiscordBot.Options;

namespace TMWRR.DiscordBot.Modules;

public sealed class Top10Module : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TmwrrClient tmwrr;
    private readonly IOptionsSnapshot<ApiOptions> apiOptions;

    public Top10Module(TmwrrClient tmwrr, IOptionsSnapshot<ApiOptions> apiOptions)
    {
        this.tmwrr = tmwrr;
        this.apiOptions = apiOptions;
    }

    [SlashCommand("2top10", "Show Top 10 records on a map")]
    public async Task Top10([Summary("map"), Autocomplete(typeof(MapAutocompleteHandler))] string mapName)
    {
        var startedAt = Stopwatch.GetTimestamp();

        var maps = await tmwrr.GetMapsAsync(mapName);

        if (!maps.Any())
        {
            await RespondAsync($"No maps found matching '{mapName}'.", ephemeral: true);
            return;
        }

        var selectedMap = maps.First();

        var map = await tmwrr.GetMapAsync(selectedMap.MapUid);

        if (map is null)
        {
            await RespondAsync($"Map '{selectedMap.Name}' not found.", ephemeral: true);
            return;
        }

        var description = BuildRecords(map, playerNoLink: false, timeNoLink: false);

        if (description.Length > 4096)
        {
            description = BuildRecords(map, playerNoLink: true, timeNoLink: false);

            if (description.Length > 4096)
            {
                description = BuildRecords(map, playerNoLink: true, timeNoLink: true);
            }
        }

        var lastUpdatedAt = default(DateTimeOffset?);

        if (map.CampaignTMF is not null)
        {
            var snapshotInfo = await tmwrr.GetLatestTMFCampaignSnapshotAsync(map.CampaignTMF.Id);
            lastUpdatedAt = snapshotInfo?.CreatedAt;
        }

        var embed = new EmbedBuilder
        {
            Title = map.GetDisplayName() + " (Official Mode)",
            Description = description,
            ThumbnailUrl = $"{apiOptions.Value.PublicBaseAddress}/{TmwrrClient.GetMapThumbnailEndpoint(map.MapUid)}",
            Footer = new EmbedFooterBuilder { Text = $"TMWR v2 (executed in {Stopwatch.GetElapsedTime(startedAt).TotalSeconds:0.00}s)" },
            Fields =
            {
                new EmbedFieldBuilder
                {
                    Name = "Record count",
                    Value = map.RecordCountTMF?.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ') ?? "(not available)",
                    IsInline = true
                }
            }
        };

        if (lastUpdatedAt.HasValue)
        {
            embed.AddField("Last updated at", TimestampTag.FromDateTimeOffset(lastUpdatedAt.Value, TimestampTagStyles.LongDateTime), inline: true);
        }

        await RespondAsync(embed: embed.Build());
    }

    private string BuildRecords(Map map, bool playerNoLink, bool timeNoLink)
    {
        var sb = new StringBuilder();

        if (map.RecordsTMF is null || map.RecordsTMF.Count == 0)
        {
            sb.Append("No records found.");
        }
        else
        {
            foreach (var rec in map.RecordsTMF)
            {
                var timestamp = rec.Replay?.Timestamp ?? rec.Ghost?.Timestamp;
                sb.AppendLine();
                sb.Append('`');
                sb.Append(rec.Rank.ToString("00"));
                sb.Append("` ");
                sb.Append(GetTimeLink(map, rec, timeNoLink));
                sb.Append(" by ");
                sb.Append(GetPlayerLink(rec.Player, playerNoLink));

                if (timestamp.HasValue)
                {
                    sb.Append(" (");

                    if (timestamp.Value < new DateTimeOffset(2012, 1, 16, 0, 0, 0, default))
                    {
                        sb.Append("before ");
                    }

                    sb.Append(TimestampTag.FromDateTimeOffset(timestamp.Value, TimestampTagStyles.ShortDate));
                    sb.Append(')');
                }
            }
        }

        return sb.ToString();
    }

    private string GetTimeLink(Map map, TMFCampaignScoresRecord record, bool noLink = false)
    {
        var hasScoreFormat = map.Mode is not null && (map.Mode.IsStunts() || map.Mode.IsPlatform());
        var isTMUF = map.Environment?.Game?.IsTMF() ?? false;

        var score = hasScoreFormat
            ? record.Score.ToString()
            : new TimeInt32(record.Score).ToString(useHundredths: isTMUF);

        if (!noLink)
        {
            if (record.Replay is not null)
            {
                return $"[`{score}`](https://3d.gbx.tools/view/replay?url={apiOptions.Value.PublicBaseAddress}/replays/{record.Replay.Guid}/download)";
            }

            if (record.Ghost is not null)
            {
                return $"[`{score}`](https://3d.gbx.tools/view/ghost?url={apiOptions.Value.PublicBaseAddress}/ghosts/{record.Ghost.Guid}/download&mapuid={map.MapUid})";
            }
        }

        return $"`{score}`";
    }

    private static string GetPlayerLink(TMFLogin player, bool noLink = false)
    {
        if (noLink)
        {
            return $"**{player.GetDisplayName()}**";
        }

        return $"[**{player.GetDisplayName()}**](<https://ul.unitedascenders.xyz/lookup?login={Uri.EscapeDataString(player.Id)}>)";
    }

    public class MapAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var map = autocompleteInteraction.Data.Current.Value.ToString() ?? "";

            await using var scope = services.CreateAsyncScope();

            var tmwrr = scope.ServiceProvider.GetRequiredService<TmwrrClient>();

            var maps = await tmwrr.GetMapsAsync(map);

            return AutocompletionResult.FromSuccess(maps.Select(x => new AutocompleteResult(x.Name, x.Name)));
        }
    }
}