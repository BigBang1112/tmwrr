using ManiaAPI.Xml.TMUF;

namespace TMWRR.Models;

public sealed class TMFCampaignScoreDiff
{
    public List<TMFCampaignScore> NewRecords { get; } = [];
    public List<(TMFCampaignScore Old, TMFCampaignScore New)> ImprovedRecords { get; } = [];
    public List<TMFCampaignScore> RemovedRecords { get; } = [];
    public List<(TMFCampaignScore Old, TMFCampaignScore New)> WorsenedRecords { get; } = [];
    public List<TMFCampaignScore> PushedOffRecords { get; } = [];

    public bool IsEmpty => 
        NewRecords.Count == 0 && 
        ImprovedRecords.Count == 0 && 
        RemovedRecords.Count == 0 && 
        WorsenedRecords.Count == 0 &&
        PushedOffRecords.Count == 0;

    private TMFCampaignScoreDiff() { }

    public static TMFCampaignScoreDiff Calculate(Leaderboard leaderboard, IDictionary<string, TMFCampaignScore> oldByLogin, IDictionary<string, TMFCampaignScore> newByLogin, bool isStunts)
    {
        var diff = new TMFCampaignScoreDiff();

        // Detect new and improved/worsened
        foreach (var (login, updated) in newByLogin)
        {
            if (!oldByLogin.TryGetValue(login, out var old))
            {
                // New record
                diff.NewRecords.Add(updated);
                continue;
            }

            // Compare by rank first, then by score if needed
            if (updated.Rank < old.Rank || (isStunts ? updated.Score > old.Score : updated.Score < old.Score))
            {
                diff.ImprovedRecords.Add((old, updated));
            }
            else if (updated.Rank > old.Rank || (isStunts ? updated.Score < old.Score : updated.Score > old.Score))
            {
                diff.WorsenedRecords.Add((old, updated));
            }
        }

        // Maybe just checking last record is enough?
        var worstScore = isStunts
            ? leaderboard.HighScores.Min(x => x.Score)
            : leaderboard.HighScores.Max(x => x.Score);

        // Detect removed or pushed off
        foreach (var (login, old) in oldByLogin)
        {
            if (newByLogin.ContainsKey(login))
            {
                continue;
            }

            if (isStunts ? old.Score <= worstScore : old.Score >= worstScore)
            {
                diff.PushedOffRecords.Add(old);
            }
            else
            {
                diff.RemovedRecords.Add(old);
            }
        }

        return diff;
    }
}
