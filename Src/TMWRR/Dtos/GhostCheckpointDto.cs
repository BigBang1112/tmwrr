using TmEssentials;

namespace TMWRR.Dtos;

public sealed class GhostCheckpointDto
{
    public TimeInt32? Time { get; set; }
    public int? StuntsScore { get; set; }
    public float? Speed { get; set; }
}
