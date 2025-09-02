namespace TMWRR.Entities;

public class ReplayGhost
{
    public int Id { get; set; }

    public required Replay Replay { get; set; }
    public required int Order { get; set; }

    public ICollection<GhostCheckpoint> Checkpoints { get; set; } = [];
}
