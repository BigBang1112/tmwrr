using System.Collections.Immutable;
using TmEssentials;

namespace TMWRR.Api.TMF;

public sealed class TMFLogin
{
    public required string Id { get; set; }
    public string? Nickname { get; set; }
    public string? NicknameDeformatted { get; set; }

    public ImmutableList<User>? Users { get; set; }

    public string GetDisplayName()
    {
        return NicknameDeformatted ?? (Nickname is null ? Id : TextFormatter.Deformat(Nickname));
    }
}