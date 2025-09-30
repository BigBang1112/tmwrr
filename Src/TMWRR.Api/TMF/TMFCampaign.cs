using System.Collections.Immutable;

namespace TMWRR.Api.TMF;

public sealed class TMFCampaign
{
    public required string Id { get; set; }
    public string? Name { get; set; }

    public ImmutableList<Map>? Maps { get; set; }
}