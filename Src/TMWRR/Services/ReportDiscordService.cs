using Microsoft.Extensions.Options;
using System.Text;
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
                .Concat(x.Diff.RemovedRecords.Select(y => y.Login)))
            .ToHashSet();

        var logins = (await loginService.GetMultipleAsync(loginSet, cancellationToken))
            .ToDictionary(x => x.Id, x => x.Nickname);
        //

        var sb = new StringBuilder();

        foreach (var report in campaignScoreDiffReports)
        {
            foreach (var newRecord in report.Diff.NewRecords)
            {
                sb.Append(report.Map.GetDeformattedName());
                sb.Append(": `");
                sb.Append(newRecord.Rank.ToString("00"));
                sb.Append("` `");
                sb.Append(newRecord.GetTime().ToString(useHundredths: true));
                sb.Append("` by ");
                sb.AppendLine(newRecord.Login);
            }

            foreach (var (oldRecord, newRecord) in report.Diff.ImprovedRecords)
            {
                sb.Append(report.Map.GetDeformattedName());
                sb.Append(": `");
                sb.Append(newRecord.Rank.ToString("00"));
                sb.Append("` `");
                sb.Append(newRecord.GetTime().ToString(useHundredths: true));
                sb.Append("` `");
                sb.Append((newRecord.GetTime() - oldRecord.GetTime()).TotalMilliseconds.ToString("0.00"));
                sb.Append("` from `");
                sb.Append(oldRecord.Rank.ToString("0.00"));
                sb.Append("` by ");
                sb.AppendLine(newRecord.Login);
            }

            foreach (var removedRecord in report.Diff.RemovedRecords)
            {
                sb.Append(report.Map.GetDeformattedName());
                sb.Append(": `");
                sb.Append(removedRecord.Rank.ToString("00"));
                sb.Append("` `");
                sb.Append(removedRecord.GetTime().ToString(useHundredths: true));
                sb.Append("` by ");
                sb.Append(removedRecord.Login);
                sb.AppendLine(" was **removed**");
            }
        }

        await webhook.SendMessageAsync(sb.ToString(), cancellationToken);
    }
}
