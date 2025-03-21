using ManiaAPI.XmlRpc.TMUF;

namespace TMWRR.Services.TMF;

internal interface IScoreCheckerService
{
    Task<DateTimeOffset?> CheckScoresAsync(CancellationToken cancellationToken);
}

internal sealed class ScoreCheckerService : IScoreCheckerService
{
    private const string EarliestZone = "World|Japan";

    private static readonly string[] Campaigns = [
        "UnitedRace",
        "UnitedPuzzle",
        "UnitedPlatform",
        "UnitedStunts",
        "Nations",
        "ManiaStar"
    ];

    private readonly MasterServerTMUF masterServer;
    private readonly ILogger<ScoreCheckerService> logger;

    public ScoreCheckerService(MasterServerTMUF masterServer, ILogger<ScoreCheckerService> logger)
    {
        this.masterServer = masterServer;
        this.logger = logger;
    }

    public async Task<DateTimeOffset?> CheckScoresAsync(CancellationToken cancellationToken)
    {
        var approxLastModifiedDateTime = default(DateTimeOffset?);

        var generalScoresInfo = await masterServer.FetchLatestGeneralScoresInfoAsync(EarliestZone, cancellationToken: cancellationToken);

        if (generalScoresInfo is null)
        {
            logger.LogWarning("Failed to retrieve info for general scores, because the scores file for this zone doesn't exist.");
            return null;
        }

        if (approxLastModifiedDateTime is null || generalScoresInfo.Value.LastModified > approxLastModifiedDateTime)
        {
            approxLastModifiedDateTime = generalScoresInfo.Value.LastModified;
        }

        var scoresNumber = generalScoresInfo.Value.Number;

        logger.LogInformation("Retrieved scores info for general scores. Number: {Number}, Approx. date: {Date}.", scoresNumber, approxLastModifiedDateTime);

        var generalScores = await masterServer.DownloadGeneralScoresAsync(scoresNumber, EarliestZone, cancellationToken);

        if (generalScores is null)
        {
            throw new InvalidOperationException("Failed to retrieve general scores. The zone was considered existing, but for the download, it doesn't.");
        }

        logger.LogInformation("Retrieved scores for general scores.");

        await ProcessGeneralScoresAsync(generalScores.Zones["World"], cancellationToken);

        foreach (var campaign in Campaigns)
        {
            var campaignScores = await masterServer.DownloadCampaignScoresAsync(campaign, scoresNumber, EarliestZone, cancellationToken);

            if (campaignScores is null)
            {
                logger.LogWarning("Failed to retrieve scores for campaign {Campaign}, because the scores file for this zone doesn't exist.", campaign);
                continue;
            }

            logger.LogInformation("Retrieved scores for campaign {Campaign}.", campaign);

            await ProcessCampaignScoresAsync(campaign, campaignScores.Maps, campaignScores.MedalZones["World"], cancellationToken);
        }

        return approxLastModifiedDateTime;
    }

    private async Task ProcessGeneralScoresAsync(Leaderboard leaderboard, CancellationToken cancellationToken)
    {

    }

    private async Task ProcessCampaignScoresAsync(
        string campaign, 
        IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps, 
        CampaignScoresMedalZone medalsWorld, 
        CancellationToken cancellationToken)
    {

    }
}
