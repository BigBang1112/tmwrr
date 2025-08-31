using Discord;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using TmEssentials;
using TMWRR.DiscordReport;
using TMWRR.Enums;
using TMWRR.Models;
using TMWRR.Options;

namespace TMWRR.Services;

public interface IReportDiscordService
{
    Task ReportAsync(DateTimeOffset reportedAt, IEnumerable<TMFCampaignScoreDiffReport> campaignScoreDiffReports, CancellationToken cancellationToken);
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

    public async Task ReportAsync(DateTimeOffset reportedAt, IEnumerable<TMFCampaignScoreDiffReport> campaignScoreDiffReports, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaignScoreDiffReports);

        logger.LogInformation("Creating Discord webhook...");

        using var webhook = webhookFactory.Create(tmufOptions.Value.ChangesDiscordWebhookUrl);

        if (!campaignScoreDiffReports.Any())
        {
            logger.LogInformation("Sending report about no changes in TMF campaigns...");
            await SendReportAsync(webhook, reportedAt, "No changes in TMF campaigns!", [], cancellationToken);
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

            foreach (var removedRecord in report.Diff.RemovedRecords)
            {
                var removedRecordNickname = logins.GetValueOrDefault(removedRecord.Login) ?? removedRecord.Login;
                var score = report.Map.IsStunts() || report.Map.IsPlatform()
                    ? removedRecord.Score.ToString()
                    : removedRecord.GetTime().ToString(useHundredths: true);

                sb.AppendFormat("`{0}` [`{1}`](<https://3d.gbx.tools/view/ghost?url=https://api.tmwrr.bigbang1112.cz/ghosts/{2}&mapuid={3}>) by **[{4}](<https://ul.unitedascenders.xyz/lookup?login={5}>)** was **removed**",
                    removedRecord.Rank.ToString("00"),
                    score,
                    removedRecord.GhostGuid, // can be null sometimes (if something breaks), be careful
                    report.Map.MapUid,
                    TextFormatter.Deformat(removedRecordNickname),
                    removedRecord.Login);
                sb.AppendLine();
            }

            foreach (var newRecord in report.Diff.NewRecords)
            {
                var newRecordNickname = logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login;
                var score = report.Map.IsStunts() || report.Map.IsPlatform()
                    ? newRecord.Score.ToString()
                    : newRecord.GetTime().ToString(useHundredths: true);
                var timestampStyle = DateTimeOffset.UtcNow - newRecord.Timestamp > TimeSpan.FromDays(1)
                    ? TimestampTagStyles.ShortDateTime
                    : TimestampTagStyles.ShortTime;
                var timestamp = newRecord.Timestamp.HasValue
                    ? $"({TimestampTag.FormatFromDateTimeOffset(newRecord.Timestamp.Value, timestampStyle)})"
                    : string.Empty;

                sb.AppendFormat("`{0}` **[`{1}`](<https://3d.gbx.tools/view/ghost?url=https://api.tmwrr.bigbang1112.cz/ghosts/{2}&mapuid={3}>)** by **[{4}](<https://ul.unitedascenders.xyz/lookup?login={5}>)** {6} [`{7} SP`]",
                    newRecord.Rank.ToString("00"),
                    score,
                    newRecord.GhostGuid, // can be null sometimes (if something breaks), be careful
                    report.Map.MapUid,
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login,
                    timestamp,
                    newRecord.Skillpoints?.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' '));
                sb.AppendLine();
            }

            foreach (var pushedOffRecord in report.Diff.PushedOffRecords)
            {
                var pushedOffRecordNickname = logins.GetValueOrDefault(pushedOffRecord.Login) ?? pushedOffRecord.Login;
                var score = report.Map.IsStunts() || report.Map.IsPlatform()
                    ? pushedOffRecord.Score.ToString()
                    : pushedOffRecord.GetTime().ToString(useHundredths: true);

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
                var score = report.Map.IsStunts() || report.Map.IsPlatform()
                    ? newRecord.Score.ToString()
                    : newRecord.GetTime().ToString(useHundredths: true);
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

                sb.AppendFormat("`{0}` **[`{1}`](<https://3d.gbx.tools/view/ghost?url=https://api.tmwrr.bigbang1112.cz/ghosts/{2}&mapuid={3}>)** `{4}` from `{5}` by **[{6}](<https://ul.unitedascenders.xyz/lookup?login={7}>)** {8} [`{10} SP`]",
                    newRecord.Rank.ToString("00"),
                    score,
                    newRecord.GhostGuid, // can be null sometimes (if something breaks), be careful
                    report.Map.MapUid,
                    delta,
                    oldRecord.Rank.ToString("00"),
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login,
                    timestamp,
                    newRecord.Skillpoints?.ToString("N0"),
                    skillpointDiffPlusStr);
                sb.AppendLine();
            }

            fields.Add(new EmbedFieldBuilder
            {
                Name = report.Map.GetDeformattedName(),
                Value = sb.ToString()
            });
        }

        var maps = campaignScoreDiffReports.Select(x =>
            string.Format("[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)", x.Map.GetDeformattedName(), x.Map.MapUid));

        logger.LogInformation("Sending report about changed maps...");

        await SendReportAsync(webhook, reportedAt, $"Solo leaderboards have changed for {string.Join(", ", maps)}.", fields, cancellationToken);
    }

    private async Task SendReportAsync(IDiscordWebhook webhook, DateTimeOffset reportedAt, string text, IEnumerable<EmbedFieldBuilder> fields, CancellationToken cancellationToken)
    {
        var embed = new EmbedBuilder()
            .WithDescription(text)
            .WithFields(fields)
            .WithColor(Color.Blue)
            .WithFooter("TMWRR (TMUF Solo Changes) Experimental")
            .WithTitle("TMWRR")
            .WithUrl("https://github.com/BigBang1112/tmwrr")
            .WithTimestamp(reportedAt)
            .Build();

        await webhook.SendMessageAsync(embed, cancellationToken); 
        
        logger.LogInformation("Discord report sent.");
    }
}
