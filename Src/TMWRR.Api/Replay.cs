using System.Collections.Immutable;

namespace TMWRR.Api;

public sealed class Replay
{
    public required Guid Guid { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? Url { get; set; }
    public ImmutableList<ReplayGhost>? Ghosts { get; set; }
}
