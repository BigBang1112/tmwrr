using System.Collections.Immutable;

namespace TMWRR.Dtos;

public sealed class ReplayGhostDto
{
    public ImmutableList<GhostCheckpointDto>? Checkpoints { get; set; }
}
