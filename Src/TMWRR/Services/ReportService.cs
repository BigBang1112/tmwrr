using TMWRR.Entities.TMF;
using TMWRR.Models;

namespace TMWRR.Services;

public interface IReportService
{
    Task ReportAsync(IReadOnlyDictionary<string, TMFCampaignScoreDiff> mapUidCampaignScoreDiffs, CancellationToken cancellationToken);
    Task ReportAsync(TMFGeneralScoresSnapshot snapshot, TMFGeneralScoreDiff? generalDiff, CancellationToken cancellationToken);
}

public sealed class ReportService : IReportService
{
    private readonly IMapService mapService;
    private readonly IReportDiscordService reportDiscordService;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ReportService> logger;

    public ReportService(
        IMapService mapService, 
        IReportDiscordService reportDiscordService, 
        TimeProvider timeProvider,
        ILogger<ReportService> logger)
    {
        this.mapService = mapService;
        this.reportDiscordService = reportDiscordService;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task ReportAsync(IReadOnlyDictionary<string, TMFCampaignScoreDiff> mapUidCampaignScoreDiffs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapUidCampaignScoreDiffs);

        logger.LogInformation("Reporting {Count} campaign score diffs...", mapUidCampaignScoreDiffs.Count);

        var reportedAt = timeProvider.GetUtcNow();

        var campaignScoreDiffReports = new List<TMFCampaignScoreDiffReport>();

        foreach (var (mapUid, diff) in mapUidCampaignScoreDiffs)
        {
            logger.LogInformation("Getting map data for map UID {MapUid}...", mapUid);

            // CAUTION this also pulls thumbnail data, might need an alternative way.
            var map = await mapService.GetOrCreateAsync(mapUid, cancellationToken);

            if (map is null)
            {
                continue; // or handle the case where the map is not found
            }

            var report = new TMFCampaignScoreDiffReport(map, diff);
            campaignScoreDiffReports.Add(report);
        }

        logger.LogInformation("Reporting {Count} campaign score diffs to Discord...", campaignScoreDiffReports.Count);

        await reportDiscordService.ReportAsync(reportedAt, campaignScoreDiffReports, cancellationToken);

        logger.LogInformation("Report completed.");
    }

    public async Task ReportAsync(TMFGeneralScoresSnapshot snapshot, TMFGeneralScoreDiff? generalDiff, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        logger.LogInformation("Reporting general score diff...");

        var reportedAt = timeProvider.GetUtcNow();

        await reportDiscordService.ReportAsync(reportedAt, snapshot, generalDiff, cancellationToken);

        logger.LogInformation("Report completed.");
    }
}
