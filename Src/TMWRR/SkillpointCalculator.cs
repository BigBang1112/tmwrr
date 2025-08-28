namespace TMWRR;

public static class SkillpointCalculator
{
    public static int[] GetRanksForSkillpoints(int[] ranks)
    {
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
