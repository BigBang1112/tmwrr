namespace TMWRR.Entities.TMF;

public sealed class TMFLadderScoresXY
{
    public int Id { get; set; }
    public required TMFLadderScoresSnapshot Snapshot { get; set; }
    public required int Rank { get; set; }
    public required int Points { get; set; }
    public required int Order { get; set; }

    public override string ToString()
    {
        return $"{Rank}) {Points} points";
    }
}