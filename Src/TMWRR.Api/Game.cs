using System.Collections.Immutable;

namespace TMWRR.Api;

public sealed class Game
{
    public required string Id { get; set; }

    public ImmutableList<TMEnvironment>? Environments { get; set; }

    public EMode GetEnum()
    {
        return Enum.Parse<EMode>(Id);
    }

    public bool IsTMF()
    {
        return Id == nameof(EGame.TMF);
    }
}
