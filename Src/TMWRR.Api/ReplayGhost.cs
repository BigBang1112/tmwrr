using System.Collections.Immutable;

namespace TMWRR.Api;

public sealed class ReplayGhost
{
    public ImmutableList<GhostCheckpoint>? Checkpoints { get; set; }
}
