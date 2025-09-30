namespace TMWRR.Api.TMF;

public sealed class TMFGeneralScoresPlayer
{
    public required int Rank { get; set; }
    public required TMFLogin Player { get; set; }
    public required int Score { get; set; }

    public override string ToString()
    {
        return $"{Rank}) {Score} by {Player.Id}";
    }
}
