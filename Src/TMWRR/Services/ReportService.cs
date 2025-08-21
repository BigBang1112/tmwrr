using TMWRR.Models;

namespace TMWRR.Services;

public interface IReportService
{
    Task ReportAsync(IReadOnlyDictionary<string, TMFCampaignScoreDiff> mapUidCampaignScoreDiffs, CancellationToken cancellationToken);
}

public sealed class ReportService : IReportService
{
    private readonly IMapService mapService;
    private readonly IReportDiscordService reportDiscordService;

    public ReportService(IMapService mapService, IReportDiscordService reportDiscordService)
    {
        this.mapService = mapService;
        this.reportDiscordService = reportDiscordService;
    }

    public async Task ReportAsync(IReadOnlyDictionary<string, TMFCampaignScoreDiff> mapUidCampaignScoreDiffs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapUidCampaignScoreDiffs, nameof(mapUidCampaignScoreDiffs));

        var campaignScoreDiffReports = new List<TMFCampaignScoreDiffReport>();

        foreach (var (mapUid, diff) in mapUidCampaignScoreDiffs)
        {
            // CAUTION this also pulls thumbnail data, might need an alternative way.
            var map = await mapService.GetOrCreateAsync(mapUid, cancellationToken);

            if (map is null)
            {
                continue; // or handle the case where the map is not found
            }

            var report = new TMFCampaignScoreDiffReport(map, diff);
            campaignScoreDiffReports.Add(report);
        }

        await reportDiscordService.ReportAsync(campaignScoreDiffReports, cancellationToken);
    }
}
