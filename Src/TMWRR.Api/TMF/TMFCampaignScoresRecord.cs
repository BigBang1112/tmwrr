namespace TMWRR.Api.TMF;

public class TMFCampaignScoresRecord
{
    public required int Rank { get; set; }
    public required int Score { get; set; }
    public required TMFLogin Player { get; set; }
    public int? Skillpoints { get; set; }
    public Ghost? Ghost { get; set; }
    public Replay? Replay { get; set; }
}
