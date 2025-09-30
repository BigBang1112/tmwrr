using TmEssentials;

namespace TMWRR.Entities;

public class GhostCheckpointEntity
{
    public int Id { get; set; }
    public TimeInt32? Time { get; set; }
    public int? StuntsScore { get; set; }
    public float? Speed { get; set; }
    public GhostEntity? Ghost { get; set; }
    public ReplayGhostEntity? ReplayGhost { get; set; }
    public required int Order { get; set; }
}
