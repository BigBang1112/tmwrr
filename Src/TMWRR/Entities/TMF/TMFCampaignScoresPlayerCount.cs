namespace TMWRR.Entities.TMF;

public class TMFCampaignScoresPlayerCount
{
    public int Id { get; set; }
    public required TMFCampaignScoresSnapshot Snapshot { get; set; }
    public required Map Map { get; set; }
    public required int Count { get; set; }
}
