namespace TMWRR.Dtos;

public class TMFCampaignScoresRecordDto
{
    public required int Rank { get; set; }
    public required int Score { get; set; }
    public required TMFLoginDto Player { get; set; }
    public DateTimeOffset? DrivenAt { get; set; }
    public required byte Order { get; set; }
}
