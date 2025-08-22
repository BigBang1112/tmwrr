using Microsoft.Extensions.Options;
using System.Text;
using TmEssentials;
using TMWRR.DiscordReport;
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

        var webhook = webhookFactory.Create(tmufOptions.Value.ChangesDiscordWebhookUrl);

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

        var logins = (await loginService.GetMultipleAsync(loginSet, cancellationToken))
            .ToDictionary(x => x.Id, x => x.Nickname);
        //

        var sb = new StringBuilder();

        foreach (var report in campaignScoreDiffReports)
        {
            foreach (var newRecord in report.Diff.NewRecords)
            {
                var newRecordNickname = logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login;

                sb.AppendFormat("**{0}**: `{1}` `{2}` by **{3}** ({4})",
                    report.Map.GetDeformattedName(),
                    newRecord.Rank.ToString("00"),
                    newRecord.GetTime().ToString(useHundredths: true),
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login);
                sb.AppendLine();
            }

            foreach (var pushedOffRecord in report.Diff.PushedOffRecords)
            {
                var pushedOffRecordNickname = logins.GetValueOrDefault(pushedOffRecord.Login) ?? pushedOffRecord.Login;
                sb.AppendFormat("**{0}**: `{1}` `{2}` by **{3}** ({4}) was **pushed off**",
                    report.Map.GetDeformattedName(),
                    pushedOffRecord.Rank.ToString("00"),
                    pushedOffRecord.GetTime().ToString(useHundredths: true),
                    TextFormatter.Deformat(pushedOffRecordNickname),
                    pushedOffRecord.Login);
                sb.AppendLine();
            }

            foreach (var (oldRecord, newRecord) in report.Diff.ImprovedRecords)
            {
                var newRecordNickname = logins.GetValueOrDefault(newRecord.Login) ?? newRecord.Login;

                sb.AppendFormat("**{0}**: `{1}` `{2}` `{3}` from `{4}` by **{5}** ({6})",
                    report.Map.GetDeformattedName(),
                    newRecord.Rank.ToString("00"),
                    newRecord.GetTime().ToString(useHundredths: true),
                    (newRecord.GetTime() - oldRecord.GetTime()).TotalSeconds.ToString("0.00"),
                    oldRecord.Rank.ToString("00"),
                    TextFormatter.Deformat(newRecordNickname),
                    newRecord.Login);
                sb.AppendLine();
            }

            foreach (var removedRecord in report.Diff.RemovedRecords)
            {
                var removedRecordNickname = logins.GetValueOrDefault(removedRecord.Login) ?? removedRecord.Login;

                sb.AppendFormat("**{0}**: `{1}` `{2}` by **{3}** ({4}) was **removed**",
                    report.Map.GetDeformattedName(),
                    removedRecord.Rank.ToString("00"),
                    removedRecord.GetTime().ToString(useHundredths: true),
                    TextFormatter.Deformat(removedRecordNickname),
                    removedRecord.Login);
            }
        }

        await webhook.SendMessageAsync(sb.ToString(), cancellationToken);
    }
}
