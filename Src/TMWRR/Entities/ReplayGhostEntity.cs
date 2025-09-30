namespace TMWRR.Entities;

public class ReplayGhostEntity
{
    public int Id { get; set; }

    public required ReplayEntity Replay { get; set; }
    public required int Order { get; set; }

    public ICollection<GhostCheckpointEntity> Checkpoints { get; set; } = [];
}
