using TmEssentials;

namespace TMWRR.Models;

public sealed record TMFCampaignScore(int Rank, int Score, string Login, int? Skillpoints)
{
    public DateTimeOffset? Timestamp { get; set; }
    public Guid? GhostGuid { get; set; }

    public TimeInt32 GetTime()
    {
        return new TimeInt32(Score);
    }
}
