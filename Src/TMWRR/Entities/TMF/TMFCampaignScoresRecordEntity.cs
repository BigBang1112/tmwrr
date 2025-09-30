using TmEssentials;

namespace TMWRR.Entities.TMF;

public class TMFCampaignScoresRecordEntity
{
    public int Id { get; set; }
    public required TMFCampaignScoresSnapshotEntity Snapshot { get; set; }
    public required MapEntity Map { get; set; }
    public required TMFLoginEntity Player { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public required int Score { get; set; }
    public required int Rank { get; set; }
    public required byte Order { get; set; }
    public GhostEntity? Ghost { get; set; }
    public ReplayEntity? Replay { get; set; }

    public TimeInt32 GetTime()
    {
        return new TimeInt32(Score);
    }

    public override string ToString()
    {
        return $"{Rank}) {Score} by {Player.Id}";
    }
}
