namespace TMWRR.Entities.TMF;

public class TMFGeneralScoresPlayerEntity
{
    public int Id { get; set; }
    public required TMFGeneralScoresSnapshotEntity Snapshot { get; set; }
    public required TMFLoginEntity Player { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public required int Score { get; set; }
    public required int Rank { get; set; }
    public required byte Order { get; set; }

    public override string ToString()
    {
        return $"{Rank}) {Score} by {Player.Id}";
    }
}
