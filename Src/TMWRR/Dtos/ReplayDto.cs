using System.Collections.Immutable;

namespace TMWRR.Dtos;

public sealed class ReplayDto
{
    public required Guid Guid { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? Url { get; set; }
    public ImmutableList<ReplayGhostDto>? Ghosts { get; set; }
}
