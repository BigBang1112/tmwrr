using System.Collections.Immutable;

namespace TMWRR.Dtos.TMF;

public sealed class TMFLoginDto
{
    public required string Id { get; set; }
    public string? Nickname { get; set; }
    public string? NicknameDeformatted { get; set; }

    public ImmutableList<UserDto>? Users { get; set; }
}