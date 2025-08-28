namespace TMWRR;

public static class SkillpointCalculator
{
    public static IList<int> GetRanksForSkillpoints(int[] ranks)
    {
        var skillpointRanks = new List<int>(ranks.Length);

        int i = 0;
        while (i < ranks.Length)
        {
            int value = ranks[i];
            int count = 1;

            // Count how many times this value repeats
            while (i + count < ranks.Length && ranks[i + count] == value)
                count++;

            // The worst rank is current index + count (since ranks are 1-based)
            int worstRank = i + count;

            // Assign worstRank to all occurrences of this tied group
            for (int j = 0; j < count; j++)
                skillpointRanks.Add(worstRank);

            i += count;
        }

        return skillpointRanks;
    }

    public static int CalculateSkillpoints(int playerCount, int rank)
    {
        return (int)MathF.Ceiling((playerCount - rank) * 100f / rank);
    }
}
