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

    private sealed record PlayerRecordMoreThanUsual(MapEntity Map, TMFCampaignScore Record, bool IsRemoved);

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

        // TODO: detect if one player mades >10 top 10 changes, and if so, use player's name as field name and list all his changes inside

        var newRecordsBySinglePlayer = campaignScoreDiffReports
            .SelectMany(x => x.Diff.NewRecords.Select(record => new PlayerRecordMoreThanUsual(x.Map, record, IsRemoved: false))
                .Concat(x.Diff.RemovedRecords.Select(record => new PlayerRecordMoreThanUsual(x.Map, record, IsRemoved: true))))
            .GroupBy(x => (x.Record.Login, x.IsRemoved))
            .Where(g => g.Count() > 10);

        var loginsWithMoreThanUsualChanges = newRecordsBySinglePlayer.Select(g => g.Key.Login);

        foreach (var recordsGroup in newRecordsBySinglePlayer)
        {
            var login = recordsGroup.Key.Login;
            var isRemoved = recordsGroup.Key.IsRemoved;

            var sb = BuildPlayerDiffText(recordsGroup.AsEnumerable(), timeNoLink: false);

            if (sb.Length == 0)
            {
                logger.LogWarning("Skipping empty report for player {Login}...", login);
                continue;
            }

            var skips = 0;
            while (sb.Length > EmbedFieldBuilder.MaxFieldValueLength)
            {
                skips++;
                sb = BuildPlayerDiffText(recordsGroup.SkipLast(skips), timeNoLink: false);
                sb.AppendLine($"...and {skips} more records");

                if (skips >= recordsGroup.Count())
                {
                    throw new InvalidOperationException("Cannot reduce player diff text to fit into embed field.");
                }
            }

            var playerName = TextFormatter.Deformat(logins.GetValueOrDefault(login) ?? login);

            fields.Add(new EmbedFieldBuilder
            {
                Name = $"{(isRemoved ? "Removed" : "New")} records by **{playerName}**",
                Value = sb.ToString()
            });
        }

        foreach (var report in campaignScoreDiffReports)
        {
            if (fields.Count >= EmbedBuilder.MaxFieldCount)
            {
                logger.LogWarning("Maximum number of embed fields reached, skipping remaining reports...");
                break;
            }

            var sb = BuildMapDiffText(report, logins, playerNoLink: false, timeNoLink: false, loginsWithMoreThanUsualChanges);

            if (sb.Length == 0)
            {
                logger.LogWarning("Skipping empty report for map {MapUid}...", report.Map.MapUid);
                continue;
            }

            if (sb.Length > EmbedFieldBuilder.MaxFieldValueLength)
            {
                sb = BuildMapDiffText(report, logins, playerNoLink: true, timeNoLink: false, loginsWithMoreThanUsualChanges);

                if (sb.Length > EmbedFieldBuilder.MaxFieldValueLength)
                {
                    sb = BuildMapDiffText(report, logins, playerNoLink: true, timeNoLink: true, loginsWithMoreThanUsualChanges);
                }
            }

            fields.Add(new EmbedFieldBuilder
            {
                Name = report.Map.GetDeformattedName(),
                Value = sb.ToString()
            });
        }

        string mapsStr;
        if (campaignScoreDiffReports.Count() > 10)
        { 
            mapsStr = $"{campaignScoreDiffReports.Count()} maps";
        }
        else
        {
            mapsStr = string.Join(", ", campaignScoreDiffReports.Select(x =>
                string.Format("[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)", x.Map.GetDeformattedName(), x.Map.MapUid)));
        }

        logger.LogInformation("Sending report about changed maps...");

        await SendReportAsync(reportedAt, $"Solo leaderboards have changed for {mapsStr}. {totalRecordCountDiffMessage}", fields, cancellationToken);
    }

    private static StringBuilder BuildPlayerDiffText(IEnumerable<PlayerRecordMoreThanUsual> records, bool timeNoLink)
    {
        var sb = new StringBuilder();

        foreach (var (map, record, _) in records)
        {
            var score = map.IsStunts() || map.IsPlatform()
                ? record.Score.ToString()
                : record.GetTime().ToString(useHundredths: true);
            var timeLink = timeNoLink ? score : GetTimeLink(map, record, score);
            sb.AppendFormat("`{0}` {1} on **{2}**",
                record.Rank.ToString("00"),
                timeLink,
                map.GetDeformattedName());
            sb.AppendLine();
        }

        return sb;
    }

    private static StringBuilder BuildMapDiffText(TMFCampaignScoreDiffReport report, IReadOnlyDictionary<string, string?> logins, bool playerNoLink, bool timeNoLink, IEnumerable<string> loginsWithMoreThanUsualChanges)
    {
        var newRecCount = 0;
        var hasManyRemovals = false;

        var sb = new StringBuilder();

        var isScore = report.Map.IsStunts() || report.Map.IsPlatform();

        foreach (var removedRecord in report.Diff.RemovedRecords)
        {
            if (loginsWithMoreThanUsualChanges.Contains(removedRecord.Login))
            {
                // Skip removed records for players with more than usual changes, they will be reported separately
                hasManyRemovals = true;
                continue;
            }

            var removedRecordNickname = TextFormatter.Deformat(logins.GetValueOrDefault(removedRecord.Login) ?? removedRecord.Login);
            var score = isScore ? removedRecord.Score.ToString() : removedRecord.GetTime().ToString(useHundredths: true);
            var timeLink = timeNoLink ? score : GetTimeLink(report.Map, removedRecord, score);
            var playerLink = playerNoLink
                ? removedRecordNickname
                : $"[{removedRecordNickname}](<https://ul.unitedascenders.xyz/lookup?login={removedRecord.Login}>)";

            sb.AppendFormat("`{0}` {1} by **{2}** was **removed**",
                removedRecord.Rank.ToString("00"),
                timeLink,
                playerLink);
            sb.AppendLine();
        }

        // Only report new records if there are not many removals, and if there are, only report new records beyond those made by players with many changes
        if (!hasManyRemovals || report.Diff.NewRecords.Count > loginsWithMoreThanUsualChanges.Count())
        {
            foreach (var newRecord in report.Diff.NewRecords)
            {
                if (loginsWithMoreThanUsualChanges.Contains(newRecord.Login))
                {
                    // Skip new records for players with more than usual changes, they will be reported separately
                    continue;
                }

                newRecCount++;

                var newRecordNickname = TextFormatter.Deformat(logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login);
                var score = isScore ? newRecord.Score.ToString() : newRecord.GetTime().ToString(useHundredths: true);
                var timestampStyle = DateTimeOffset.UtcNow - newRecord.Timestamp > TimeSpan.FromDays(1)
                    ? TimestampTagStyles.ShortDateTime
                    : TimestampTagStyles.ShortTime;
                var timestamp = newRecord.Timestamp.HasValue
                    ? $"({TimestampTag.FormatFromDateTimeOffset(newRecord.Timestamp.Value, timestampStyle)})"
                    : string.Empty;
                var timeLink = timeNoLink ? score : GetTimeLink(report.Map, newRecord, score);
                var playerLink = playerNoLink
                    ? newRecordNickname
                    : $"[{newRecordNickname}](<https://ul.unitedascenders.xyz/lookup?login={newRecord.Login}>)";

                sb.AppendFormat("`{0}` **{1}** by **{2}** {3} [`{4} SP`]",
                    newRecord.Rank.ToString("00"),
                    timeLink,
                    playerLink,
                    timestamp,
                    newRecord.Skillpoints?.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' '));
                sb.AppendLine();
            }
        }

        foreach (var (oldRecord, newRecord) in report.Diff.ImprovedRecords)
        {
            var improvedRecordNickname = TextFormatter.Deformat(logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login);
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
            var timeLink = timeNoLink ? score : GetTimeLink(report.Map, newRecord, score);
            var playerLink = playerNoLink
                ? improvedRecordNickname
                : $"[{improvedRecordNickname}](<https://ul.unitedascenders.xyz/lookup?login={newRecord.Login}>)";

            sb.AppendFormat("`{0}` **{1}** `{2}` from `{3}` by **{4}** {5} [`{7} SP`]",
                newRecord.Rank.ToString("00"),
                timeLink,
                delta,
                oldRecord.Rank.ToString("00"),
                playerLink,
                timestamp,
                newRecord.Skillpoints?.ToString("N0"),
                skillpointDiffPlusStr);
            sb.AppendLine();
        }

        // do not report pushed off recs when there are no new recs to report (they may have been reported separately)
        if (newRecCount > 0)
        {
            foreach (var pushedOffRecord in report.Diff.PushedOffRecords)
            {
                var pushedOffRecordNickname = TextFormatter.Deformat(logins.GetValueOrDefault(pushedOffRecord.Login) ?? pushedOffRecord.Login);
                var score = isScore ? pushedOffRecord.Score.ToString() : pushedOffRecord.GetTime().ToString(useHundredths: true);
                var playerLink = playerNoLink
                    ? pushedOffRecordNickname
                    : $"[{pushedOffRecordNickname}](<https://ul.unitedascenders.xyz/lookup?login={pushedOffRecord.Login}>)";

                sb.AppendFormat("-# `{0}` `{1}` by {2} was pushed off",
                    pushedOffRecord.Rank.ToString("00"),
                    score,
                    playerLink);
                sb.AppendLine();
            }
        }

        return sb;
    }

    private static string GetTimeLink(MapEntity map, TMFCampaignScore record, string score)
    {
        if (record.ReplayGuid.HasValue)
        {
            return $"[`{score}`](https://tmwrr.bigbang1112.cz/v/r/{GuidHelpers.ToBase64String(record.ReplayGuid.Value)})";
        }

        if (record.GhostGuid.HasValue)
        {
            return $"[`{score}`](https://tmwrr.bigbang1112.cz/v/g/{GuidHelpers.ToBase64String(record.GhostGuid.Value)}/{map.MapUid})";
        }

        return $"`{score}`";
    }

    private async Task SendReportAsync(DateTimeOffset reportedAt, string text, IEnumerable<EmbedFieldBuilder> fields, CancellationToken cancellationToken)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("TMWRR")
            .WithDescription(text)
            .WithFields(fields)
            .WithColor(Color.Blue)
            .WithFooter("TMWRR (TMUF Solo Changes) Experimental")
            .WithUrl("https://github.com/BigBang1112/tmwrr")
            .WithTimestamp(reportedAt);

        Embed finalEmbed;
        var removedFields = new List<EmbedFieldBuilder>();
        while (true)
        {
            try
            {
                finalEmbed = embedBuilder.Build();
                break;
            }
            catch (InvalidOperationException)
            {
                if (embedBuilder.Fields.Count == 0)
                {
                    logger.LogError("Cannot build embed even after removing all fields.");
                    throw;
                }

                // Remove the last field and try again
                removedFields.Add(embedBuilder.Fields.Last());
                embedBuilder.Fields.RemoveAt(embedBuilder.Fields.Count - 1);
                logger.LogWarning("Embed fields exceed maximum, removing last field and retrying... Removed fields so far: {RemovedFieldsCount}", removedFields.Count);
            }
        }

        var embeds = new List<Embed> { finalEmbed };

        foreach (var removedFieldsChunk in removedFields.Chunk(EmbedBuilder.MaxFieldCount))
        {
            embeds.Add(new EmbedBuilder()
                .WithTitle("More changes")
                .WithFields(removedFieldsChunk)
                .WithColor(Color.Orange)
                .WithFooter("TMWRR (TMUF Solo Changes) Experimental")
                .WithTimestamp(reportedAt)
                .Build());
        }

        using var webhook = webhookFactory.Create(tmufOptions.Value.Discord.TestWebhookUrl);

        foreach (var embed in embeds)
        {
            await webhook.SendMessageAsync(embed, cancellationToken);
        }

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

            sb.AppendFormat("- {0,2} | {1,-16}   {2,11}   {3,-11}\n",
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