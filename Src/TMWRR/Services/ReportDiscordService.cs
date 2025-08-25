using Discord;
using Microsoft.Extensions.Options;
using System.Text;
using TmEssentials;
using TMWRR.DiscordReport;
using TMWRR.Enums;
using TMWRR.Models;
using TMWRR.Options;

namespace TMWRR.Services;

public interface IReportDiscordService
{
    Task ReportAsync(IEnumerable<TMFCampaignScoreDiffReport> campaignScoreDiffReports, CancellationToken cancellationToken);
}

public class ReportDiscordService : IReportDiscordService
{
    private readonly IDiscordWebhookFactory webhookFactory;
    private readonly ILoginService loginService;
    private readonly IOptionsSnapshot<TMUFOptions> tmufOptions;

    public ReportDiscordService(IDiscordWebhookFactory webhookFactory, ILoginService loginService, IOptionsSnapshot<TMUFOptions> tmufOptions)
    {
        this.webhookFactory = webhookFactory;
        this.loginService = loginService;
        this.tmufOptions = tmufOptions;
    }

    public async Task ReportAsync(IEnumerable<TMFCampaignScoreDiffReport> campaignScoreDiffReports, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(campaignScoreDiffReports, nameof(campaignScoreDiffReports));

        using var webhook = webhookFactory.Create(tmufOptions.Value.ChangesDiscordWebhookUrl);

        if (!campaignScoreDiffReports.Any())
        {
            await webhook.SendMessageAsync("No changes in TMF campaigns!", cancellationToken);
            return;
        }

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

        var sb = new StringBuilder();

        foreach (var report in campaignScoreDiffReports)
        {
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
                    ? TimestampTag.FormatFromDateTimeOffset(newRecord.Timestamp.Value, timestampStyle)
                    : string.Empty;

                sb.AppendFormat("**[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)**: `{2}` `{3}` by **[{4}](<https://ul.unitedascenders.xyz/lookup?login={5}>)** {6}",
                    report.Map.GetDeformattedName(),
                    report.Map.MapUid,
                    newRecord.Rank.ToString("00"),
                    score,
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login,
                    timestamp);
                sb.AppendLine();
            }

            foreach (var pushedOffRecord in report.Diff.PushedOffRecords)
            {
                var pushedOffRecordNickname = logins.GetValueOrDefault(pushedOffRecord.Login) ?? pushedOffRecord.Login;
                var score = report.Map.IsStunts() || report.Map.IsPlatform()
                    ? pushedOffRecord.Score.ToString()
                    : pushedOffRecord.GetTime().ToString(useHundredths: true);

                sb.AppendFormat("**[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)**: `{2}` `{3}` by **[{4}](<https://ul.unitedascenders.xyz/lookup?login={5}>)** was **pushed off**",
                    report.Map.GetDeformattedName(),
                    report.Map.MapUid,
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
                    _ => (newRecord.GetTime() - oldRecord.GetTime()).TotalSeconds.ToString("0.00")
                };
                var timestampStyle = DateTimeOffset.UtcNow - newRecord.Timestamp > TimeSpan.FromDays(1)
                    ? TimestampTagStyles.ShortDateTime
                    : TimestampTagStyles.ShortTime;
                var timestamp = newRecord.Timestamp.HasValue
                    ? TimestampTag.FormatFromDateTimeOffset(newRecord.Timestamp.Value, timestampStyle)
                    : string.Empty;

                sb.AppendFormat("**[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)**: `{2}` `{3}` `{4}` from `{5}` by **[{6}](<https://ul.unitedascenders.xyz/lookup?login={7}>)** {8}",
                    report.Map.GetDeformattedName(),
                    report.Map.MapUid,
                    newRecord.Rank.ToString("00"),
                    score,
                    delta,
                    oldRecord.Rank.ToString("00"),
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login,
                    timestamp);
                sb.AppendLine();
            }

            foreach (var removedRecord in report.Diff.RemovedRecords)
            {
                var removedRecordNickname = logins.GetValueOrDefault(removedRecord.Login) ?? removedRecord.Login;
                var score = report.Map.IsStunts() || report.Map.IsPlatform()
                    ? removedRecord.Score.ToString()
                    : removedRecord.GetTime().ToString(useHundredths: true);

                sb.AppendFormat("**[{0}](<https://ul.unitedascenders.xyz/leaderboards/tracks/{1}>)**: `{2}` `{3}` by **[{4}](<https://ul.unitedascenders.xyz/lookup?login={5}>)** was **removed**",
                    report.Map.GetDeformattedName(),
                    report.Map.MapUid,
                    removedRecord.Rank.ToString("00"),
                    score,
                    TextFormatter.Deformat(removedRecordNickname),
                    removedRecord.Login);
            }
        }

        await webhook.SendMessageAsync(sb.ToString(), cancellationToken);
    }
}
