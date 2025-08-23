using System.Collections.Immutable;

namespace TMWRR.Dtos;

public sealed class GameDto
{
    public required string Id { get; set; }

    public ImmutableList<TMEnvironmentDto>? Environments { get; set; }
}
