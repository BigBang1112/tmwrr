using System.Collections.Immutable;

namespace TMWRR.Dtos;

public sealed class TMFLoginDto
{
    public required string Id { get; set; }
    public string? Nickname { get; set; }

    public ImmutableArray<UserDto> Users { get; set; } = [];
}