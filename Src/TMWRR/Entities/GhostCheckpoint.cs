using TmEssentials;

namespace TMWRR.Entities;

public class GhostCheckpoint
{
    public int Id { get; set; }
    public TimeInt32? Time { get; set; }
    public int? StuntsScore { get; set; }
    public float? Speed { get; set; }
    public Ghost? Ghost { get; set; }
}
