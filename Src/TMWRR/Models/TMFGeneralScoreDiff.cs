using ManiaAPI.Xml.TMUF;

namespace TMWRR.Models;

public sealed class TMFGeneralScoreDiff
{
    public List<TMFGeneralScore> NewPlayers { get; } = [];
    public List<(TMFGeneralScore Old, TMFGeneralScore New)> ImprovedPlayers { get; } = [];
    public List<TMFGeneralScore> RemovedPlayers { get; } = [];
    public List<(TMFGeneralScore Old, TMFGeneralScore New)> WorsenedPlayers { get; } = [];
    public List<TMFGeneralScore> PushedOffPlayers { get; } = [];

    public bool IsEmpty => 
        NewPlayers.Count == 0 && 
        ImprovedPlayers.Count == 0 && 
        RemovedPlayers.Count == 0 && 
        WorsenedPlayers.Count == 0 &&
        PushedOffPlayers.Count == 0;

    private TMFGeneralScoreDiff() { }

    public static TMFGeneralScoreDiff Calculate(Leaderboard leaderboard, IDictionary<string, TMFGeneralScore> oldByLogin, IDictionary<string, TMFGeneralScore> newByLogin)
    {
        var diff = new TMFGeneralScoreDiff();

        // Detect new and improved/worsened
        foreach (var (login, updated) in newByLogin)
        {
            if (!oldByLogin.TryGetValue(login, out var old))
            {
                // New record
                diff.NewPlayers.Add(updated);
                continue;
            }

            // Compare by rank first, then by score if needed
            if (updated.Rank < old.Rank || updated.Score > old.Score)
            {
                diff.ImprovedPlayers.Add((old, updated));
            }
            else if (updated.Rank > old.Rank || updated.Score < old.Score)
            {
                diff.WorsenedPlayers.Add((old, updated));
            }
        }

        // Maybe just checking last record is enough?
        var worstScore = leaderboard.HighScores.Min(x => x.Score);

        // Detect removed or pushed off
        foreach (var (login, old) in oldByLogin)
        {
            if (newByLogin.ContainsKey(login))
            {
                continue;
            }

            if (old.Score <= worstScore)
            {
                diff.PushedOffPlayers.Add(old);
            }
            else
            {
                diff.RemovedPlayers.Add(old);
            }
        }

        return diff;
    }
}
