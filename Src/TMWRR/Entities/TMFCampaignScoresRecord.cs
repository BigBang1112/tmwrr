using TmEssentials;

namespace TMWRR.Entities;

public class TMFCampaignScoresRecord
{
    public int Id { get; set; }
    public required TMFCampaignScoresSnapshot Snapshot { get; set; }
    public required Map Map { get; set; }
    public required TMFLogin Player { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public required int Score { get; set; }
    public required int Rank { get; set; }
    public required byte Order { get; set; }
    public TMFReplay? Replay { get; set; }

    public TimeInt32 GetTime()
    {
        return new TimeInt32(Score);
    }

    public override string ToString()
    {
        return $"{Rank}) {Score} by {Player.Id}";
    }
}
