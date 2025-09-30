using System.Collections.Immutable;

namespace TMWRR.Api;

public sealed class Game
{
    public required string Id { get; set; }

    public ImmutableList<TMEnvironment>? Environments { get; set; }
}
