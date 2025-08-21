using TmEssentials;

namespace TMWRR.Models;

public record TMFCampaignScore(int Rank, int Score, string Login)
{
    public TimeInt32 GetTime()
    {
        return new TimeInt32(Score);
    }
}
