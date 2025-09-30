using System.Collections.Immutable;

namespace TMWRR.Api.TMF;

public sealed class TMFGeneralScoresSnapshot
{
    public Guid? Guid { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset PublishedAt { get; set; }
    public bool NoChanges { get; set; }
    public required int PlayerCount { get; set; }
    public ImmutableList<TMFGeneralScoresPlayer>? Players { get; set; }
}
