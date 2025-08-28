namespace TMWRR.Dtos.TMF;

public class TMFCampaignScoresRecordDto
{
    public required int Rank { get; set; }
    public required int Score { get; set; }
    public required TMFLoginDto Player { get; set; }
    public int? Skillpoints { get; set; }
    public GhostDto? Ghost { get; set; }
    public required byte Order { get; set; }
}
