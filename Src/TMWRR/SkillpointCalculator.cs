namespace TMWRR;

public static class SkillpointCalculator
{
    public static int[] GetRanksForSkillpoints(int[] ranks)
    {
        // TODO: ranks could be wrong if there are ties above the last rank in the array equaling the last rank
        // use Skillpoints Count value from scores to figure out the true amount of ties
        // possibly needs another column for PlayerCount called LastRankCount that's passed into this

        var skillpointRanks = new int[ranks.Length];
        var outIdx = 0;
        var i = 0;
        while (i < ranks.Length)
        {
            var value = ranks[i];
            var count = 1;

            // Count how many times this value repeats
            while (i + count < ranks.Length && ranks[i + count] == value)
                count++;

            // The worst rank is the current index + count (ranks are 1-based)
            var worstRank = i + count;

            // Assign worstRank to all occurrences of this tied group
            for (int j = 0; j < count; j++)
                skillpointRanks[outIdx++] = worstRank;

            i += count;
        }

        return skillpointRanks;
    }

    public static int CalculateSkillpoints(int playerCount, int rank)
    {
        return (int)MathF.Ceiling((playerCount - rank) * 100f / rank);
    }
}
