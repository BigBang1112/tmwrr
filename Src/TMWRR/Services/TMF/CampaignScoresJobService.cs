using ManiaAPI.Xml.TMUF;
using TMWRR.Data;

namespace TMWRR.Services.TMF;

public interface ICampaignScoresJobService
{
    Task ProcessAsync(string campaignId, IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps, CampaignScoresMedalZone medals, CancellationToken cancellationToken);
}

public class CampaignScoresJobService : ICampaignScoresJobService
{
    private readonly AppDbContext db;

    public CampaignScoresJobService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task ProcessAsync(
        string campaignId,
        IReadOnlyDictionary<string, CampaignScoresLeaderboard> maps,
        CampaignScoresMedalZone medals,
        CancellationToken cancellationToken)
    {

    }
}
