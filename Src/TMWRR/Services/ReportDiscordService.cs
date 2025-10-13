using Discord;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using TmEssentials;
using TMWRR.Api;
using TMWRR.DiscordReport;
using TMWRR.Entities;
using TMWRR.Entities.TMF;
using TMWRR.Models;
using TMWRR.Options;

namespace TMWRR.Services;

public interface IReportDiscordService
{
    Task ReportAsync(DateTimeOffset reportedAt, IEnumerable<TMFCampaignScoreDiffReport> campaignScoreDiffReports, IReadOnlyDictionary<string, PlayerCountDiff> recordCountDiffsByCampaignId, CancellationToken cancellationToken);
    Task ReportAsync(DateTimeOffset reportedAt, TMFGeneralScoresSnapshotEntity snapshot, TMFGeneralScoreDiff? generalDiff, CancellationToken cancellationToken);
}

public class ReportDiscordService : IReportDiscordService
{
    private readonly IDiscordWebhookFactory webhookFactory;
    private readonly ILoginService loginService;
    private readonly IOptionsSnapshot<TMUFOptions> tmufOptions;
    private readonly ILogger<ReportDiscordService> logger;

    public ReportDiscordService(
        IDiscordWebhookFactory webhookFactory,
        ILoginService loginService,
        IOptionsSnapshot<TMUFOptions> tmufOptions,
        ILogger<ReportDiscordService> logger)
    {
        this.webhookFactory = webhookFactory;
        this.loginService = loginService;
        this.tmufOptions = tmufOptions;
        this.logger = logger;
    }

    public async Task ReportAsync(
        DateTimeOffset reportedAt, 
        IEnumerable<TMFCampaignScoreDiffReport> campaignScoreDiffReports, 
        IReadOnlyDictionary<string, PlayerCountDiff> recordCountDiffsByCampaignId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaignScoreDiffReports);

        var totalRecordCountDiff = recordCountDiffsByCampaignId.Values.Sum(x => x.CountAfter - x.CountBefore.GetValueOrDefault());

        string totalRecordCountDiffMessage;
        if (totalRecordCountDiff > 0)
        {
            totalRecordCountDiffMessage = $"{totalRecordCountDiff} record{(totalRecordCountDiff == 1 ? " has" : "s have")} been driven (for the first time).";
        }
        else if (totalRecordCountDiff < 0)
        {
            totalRecordCountDiffMessage = $"At least {-totalRecordCountDiff} record{(totalRecordCountDiff == -1 ? " has" : "s have")} been removed.";
        }
        else
        {
            totalRecordCountDiffMessage = "No new records has been driven (for the first time).";
        }

        logger.LogInformation("Creating Discord webhook...");

        if (!campaignScoreDiffReports.Any())
        {
            logger.LogInformation("Sending report about no changes in TMF campaigns...");
            await SendReportAsync(reportedAt, $"No visible changes in TMF campaigns! {totalRecordCountDiffMessage}", [], cancellationToken);
            return;
        }

        logger.LogInformation("Resolving logins once more...");

        // TODO: TMFLogin should be probably given directly by the TMFCampaignScoreDiffReport model
        var loginSet = campaignScoreDiffReports
            .SelectMany(x =>
                x.Diff.NewRecords.Select(x => x.Login)
                .Concat(x.Diff.ImprovedRecords.Select(y => y.New.Login))
                .Concat(x.Diff.RemovedRecords.Select(y => y.Login))
                .Concat(x.Diff.PushedOffRecords.Select(y => y.Login)))
            .ToHashSet();

        var logins = (await loginService.GetMultipleTMFAsync(loginSet, cancellationToken))
            .ToDictionary(x => x.Id, x => x.Nickname);
        //

        logger.LogInformation("Building report message...");

        var fields = new List<EmbedFieldBuilder>();

        foreach (var report in campaignScoreDiffReports)
        {
            var sb = new StringBuilder();

            var isScore = report.Map.IsStunts() || report.Map.IsPlatform();

            foreach (var removedRecord in report.Diff.RemovedRecords)
            {
                var removedRecordNickname = logins.GetValueOrDefault(removedRecord.Login) ?? removedRecord.Login;
                var score = isScore ? removedRecord.Score.ToString() : removedRecord.GetTime().ToString(useHundredths: true);
                var timeLink = GetTimeLink(report.Map, removedRecord, score);

                sb.AppendFormat("`{0}` {1} by **[{2}](<https://ul.unitedascenders.xyz/lookup?login={3}>)** was **removed**",
                    removedRecord.Rank.ToString("00"),
                    timeLink,
                    TextFormatter.Deformat(removedRecordNickname),
                    removedRecord.Login);
                sb.AppendLine();
            }

            foreach (var newRecord in report.Diff.NewRecords)
            {
                var newRecordNickname = logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login;
                var score = isScore ? newRecord.Score.ToString() : newRecord.GetTime().ToString(useHundredths: true);
                var timestampStyle = DateTimeOffset.UtcNow - newRecord.Timestamp > TimeSpan.FromDays(1)
                    ? TimestampTagStyles.ShortDateTime
                    : TimestampTagStyles.ShortTime;
                var timestamp = newRecord.Timestamp.HasValue
                    ? $"({TimestampTag.FormatFromDateTimeOffset(newRecord.Timestamp.Value, timestampStyle)})"
                    : string.Empty;
                var timeLink = GetTimeLink(report.Map, newRecord, score);

                sb.AppendFormat("`{0}` **{1}** by **[{2}](<https://ul.unitedascenders.xyz/lookup?login={3}>)** {4} [`{5} SP`]",
                    newRecord.Rank.ToString("00"),
                    timeLink,
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login,
                    timestamp,
                    newRecord.Skillpoints?.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' '));
                sb.AppendLine();
            }

            foreach (var pushedOffRecord in report.Diff.PushedOffRecords)
            {
                var pushedOffRecordNickname = logins.GetValueOrDefault(pushedOffRecord.Login) ?? pushedOffRecord.Login;
                var score = isScore ? pushedOffRecord.Score.ToString() : pushedOffRecord.GetTime().ToString(useHundredths: true);

                sb.AppendFormat("-# `{0}` `{1}` by [{2}](<https://ul.unitedascenders.xyz/lookup?login={3}>) was pushed off",
                    pushedOffRecord.Rank.ToString("00"),
                    score,
                    TextFormatter.Deformat(pushedOffRecordNickname),
                    pushedOffRecord.Login);
                sb.AppendLine();
            }

            foreach (var (oldRecord, newRecord) in report.Diff.ImprovedRecords)
            {
                var newRecordNickname = logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login;
                var score = isScore ? newRecord.Score.ToString() : newRecord.GetTime().ToString(useHundredths: true);
                var delta = report.Map.GetMode() switch
                {
                    EMode.Stunts => $"+{newRecord.Score - oldRecord.Score}",
                    EMode.Platform => (newRecord.Score - oldRecord.Score).ToString(),
                    _ => (newRecord.GetTime() - oldRecord.GetTime()).TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture)
                };
                var timestampStyle = DateTimeOffset.UtcNow - newRecord.Timestamp > TimeSpan.FromDays(1)
                    ? TimestampTagStyles.ShortDateTime
                    : TimestampTagStyles.ShortTime;
                var timestamp = newRecord.Timestamp.HasValue
                    ? $"({TimestampTag.FormatFromDateTimeOffset(newRecord.Timestamp.Value, timestampStyle)})"
                    : string.Empty;
                var skillpointDiff = newRecord.Skillpoints.GetValueOrDefault() - oldRecord.Skillpoints.GetValueOrDefault();
                var skillpointDiffStr = skillpointDiff.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
                var skillpointDiffPlusStr = skillpointDiff >= 0 ? $"+{skillpointDiffStr}" : skillpointDiffStr;
                var timeLink = GetTimeLink(report.Map, newRecord, score);

                sb.AppendFormat("`{0}` **{1}** `{2}` from `{3}` by **[{4}](<https://ul.unitedascenders.xyz/lookup?login={5}>)** {6} [`{8} SP`]",
                    newRecord.Rank.ToString("00"),
                    timeLink,
                    delta,
                    oldRecord.Rank.ToString("00"),
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login,
                    timestamp,
                    newRecord.Skillpoints?.ToString("N0"),
                    skillpointDiffPlusStr);
                sb.AppendLine();
            }

            // Field value length must be less than or equal to 1024
            fields.Add(new EmbedFieldBuilder
            {
                Name = report.Map.GetDeformattedName(),
                Value = sb.ToString()
            });
        }

        var maps = campaignScoreDiffReports.Select(x =>
            string.Format("[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)", x.Map.GetDeformattedName(), x.Map.MapUid));

        logger.LogInformation("Sending report about changed maps...");

        await SendReportAsync(reportedAt, $"Solo leaderboards have changed for {string.Join(", ", maps)}. {totalRecordCountDiffMessage}", fields, cancellationToken);
    }

    private static string GetTimeLink(MapEntity map, TMFCampaignScore record, string score)
    {
        if (record.ReplayGuid is not null)
        {
            var downloadUrl = $"https://api.tmwrr.bigbang1112.cz/replays/{record.ReplayGuid}/download";
            var encodedDownloadUrl = Uri.EscapeDataString(downloadUrl);
            return $"[`{score}`](https://3d.gbx.tools/view/replay?url={encodedDownloadUrl})";
        }

        if (record.GhostGuid is not null)
        {
            var downloadUrl = $"https://api.tmwrr.bigbang1112.cz/ghosts/{record.GhostGuid}/download";
            var encodedDownloadUrl = Uri.EscapeDataString(downloadUrl);
            var encodedMapUid = Uri.EscapeDataString(map.MapUid);
            return $"[`{score}`](https://3d.gbx.tools/view/ghost?url={encodedDownloadUrl}&mapuid={encodedMapUid})";
        }

        return $"`{score}`";
    }

    private async Task SendReportAsync(DateTimeOffset reportedAt, string text, IEnumerable<EmbedFieldBuilder> fields, CancellationToken cancellationToken)
    {
        // Total embed length must be less than or equal to 6000.

        var embed = new EmbedBuilder()
            .WithDescription(text)
            .WithFields(fields)
            .WithColor(Color.Blue)
            .WithFooter("TMWRR (TMUF Solo Changes) Experimental")
            .WithTitle("TMWRR")
            .WithUrl("https://github.com/BigBang1112/tmwrr")
            .WithTimestamp(reportedAt)
            .Build();

        using var webhook = webhookFactory.Create(tmufOptions.Value.Discord.TestWebhookUrl);

        await webhook.SendMessageAsync(embed, cancellationToken);

        logger.LogInformation("Discord report sent.");
    }

    public async Task ReportAsync(DateTimeOffset reportedAt, TMFGeneralScoresSnapshotEntity snapshot, TMFGeneralScoreDiff? generalDiff, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        logger.LogInformation("Creating Discord webhook...");

        if (snapshot.NoChanges || generalDiff is null)
        {
            logger.LogInformation("Sending report about no changes in TMF general scores...");
            await SendReportAsync(reportedAt, "No changes in TMF skillpoint leaderboard!", [], cancellationToken);
            return;
        }

        logger.LogInformation("Resolving logins once more...");

        // TODO: TMFLogin should be probably given directly by the TMFCampaignScoreDiffReport model
        var loginSet = snapshot.Players
            .Select(x => x.PlayerId)
            .ToHashSet();

        var logins = (await loginService.GetMultipleTMFAsync(loginSet, cancellationToken))
            .ToDictionary(x => x.Id, x => x.Nickname);
        //

        logger.LogInformation("Building report message...");

        /*
        var fields = new List<EmbedFieldBuilder>();

        var rankSb = new StringBuilder();

        foreach (var player in snapshot.Players.OrderBy(x => x.Order))
        { 
            rankSb.AppendLine(player.Rank.ToString("00"));
        }

        var playerSb = new StringBuilder();
        foreach (var player in snapshot.Players.OrderBy(x => x.Order))
        {
            var playerName = logins.GetValueOrDefault(player.PlayerId) ?? player.PlayerId;
            playerSb.AppendLine(TextFormatter.Deformat(playerName));
        }

        var scoreSb = new StringBuilder();
        foreach (var player in snapshot.Players.OrderBy(x => x.Order))
        {
            scoreSb.AppendLine(player.Score.ToString("N0"));
        }

        fields.Add(new EmbedFieldBuilder
        {
            Name = "Rank",
            Value = rankSb.ToString(),
            IsInline = true
        });
        fields.Add(new EmbedFieldBuilder
        {
            Name = "Player",
            Value = playerSb.ToString(),
            IsInline = true
        });
        fields.Add(new EmbedFieldBuilder
        {
            Name = "Score",
            Value = scoreSb.ToString(),
            IsInline = true
        });

        var description = $"General leaderboard has changed with {generalDiff.PlayerCountDelta:+#;-#;0} player(s).";

        await SendReportAsync(webhook, reportedAt, description, fields, cancellationToken);*/

        var sb = new StringBuilder();
        sb.AppendLine("Skillpoint leaderboard has changed.");
        sb.AppendLine("```diff");
        sb.AppendLine("     | Player             Skillpoints   Difference");
        sb.AppendLine("-----|---------------------------------------------");

        foreach (var player in snapshot.Players.OrderBy(x => x.Order))
        {
            var playerName = SimplifyUnicode(TextFormatter.Deformat(logins.GetValueOrDefault(player.PlayerId) ?? player.PlayerId));
            
            if (playerName.Length > 16)
            {
                playerName = playerName[..16] + "…";
            }

            var diffMark = ' ';
            var diffValue = "+0";

            if (generalDiff.NewPlayers.Any(x => x.Login == player.PlayerId))
            {
                diffMark = '!';
                diffValue = "NEW";
            }
            else if (generalDiff.ImprovedPlayers.Any(x => x.New.Login == player.PlayerId))
            {
                diffMark = '+';
                var (oldScore, newScore) = generalDiff.ImprovedPlayers.First(x => x.New.Login == player.PlayerId);
                var delta = newScore.Score - oldScore.Score;
                diffValue = delta < 0 ? delta.ToString("N0").Replace(',', ' ') : $"+{delta.ToString("N0").Replace(',', ' ')}";
            }
            else if (generalDiff.WorsenedPlayers.Any(x => x.New.Login == player.PlayerId))
            {
                diffMark = '-';
                var (oldScore, newScore) = generalDiff.WorsenedPlayers.First(x => x.New.Login == player.PlayerId);
                var delta = newScore.Score - oldScore.Score;
                diffValue = delta < 0 ? delta.ToString("N0").Replace(',', ' ') : $"+{delta.ToString("N0").Replace(',', ' ')}";
            }

            sb.AppendFormat("{0} {1,2} | {2,-17}  {3,11}   {4,-11}\n",
                diffMark,
                player.Rank,
                playerName,
                player.Score.ToString("N0").Replace(',', ' '),
                diffValue);
        }

        foreach (var removedPlayer in generalDiff.RemovedPlayers)
        {
            var playerName = SimplifyUnicode(TextFormatter.Deformat(logins.GetValueOrDefault(removedPlayer.Login) ?? removedPlayer.Login));
            
            if (playerName.Length > 16)
            {
                playerName = playerName[..16] + "…";
            }

            sb.AppendFormat("- {0,2} | -- | {1,-16}   {2,11}   {3,-11}\n",
                removedPlayer.Rank,
                playerName,
                removedPlayer.Score.ToString("N0").Replace(',', ' '),
                "REMOVED");
        }

        foreach (var pushedOffPlayer in generalDiff.PushedOffPlayers)
        {
            var playerName = SimplifyUnicode(TextFormatter.Deformat(logins.GetValueOrDefault(pushedOffPlayer.Login) ?? pushedOffPlayer.Login));

            if (playerName.Length > 16)
            {
                playerName = playerName[..16] + "…";
            }

            sb.AppendFormat("- {0,2} | -- | {1,-16}   {2,11}   {3,-11}\n",
                pushedOffPlayer.Rank,
                playerName,
                pushedOffPlayer.Score.ToString("N0").Replace(',', ' '),
                "PUSHED OFF");
        }

        sb.AppendLine("```");
        sb.AppendLine("*(on phone, rotate Discord horizontally for clean overview)*");
        sb.AppendLine();

        if (generalDiff.PlayerCountDelta > 0)
        {
            sb.AppendLine($"+{generalDiff.PlayerCountDelta} player{(generalDiff.PlayerCountDelta != 1 ? "s" : "")} joined the leaderboard!");
        } 
        else if (generalDiff.PlayerCountDelta < 0)
        {
            sb.AppendLine($"**At least {-generalDiff.PlayerCountDelta} player{(generalDiff.PlayerCountDelta != -1 ? "s" : "")} left the leaderboard!**");
        }
        else
        {
            sb.AppendLine("No change in player count.");
        }

        logger.LogInformation("Sending report about changed general scores...");

        await SendReportAsync(reportedAt, sb.ToString(), [], cancellationToken);
    }

    private static string SimplifyUnicode(string input)
    {
        // Normalize to decomposition form (base char + combining marks)
        string normalized = input.Normalize();

        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark &&
                cat != UnicodeCategory.EnclosingMark &&
                cat != UnicodeCategory.Format)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}