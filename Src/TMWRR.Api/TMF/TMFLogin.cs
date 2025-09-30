using System.Collections.Immutable;

namespace TMWRR.Api.TMF;

public sealed class TMFLogin
{
    public required string Id { get; set; }
    public string? Nickname { get; set; }
    public string? NicknameDeformatted { get; set; }

    public ImmutableList<User>? Users { get; set; }
}