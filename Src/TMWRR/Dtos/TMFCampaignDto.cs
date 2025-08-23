using System.Collections.Immutable;

namespace TMWRR.Dtos;

public sealed class TMFCampaignDto
{
    public required string Id { get; set; }
    public string? Name { get; set; }

    public ImmutableList<MapDto>? Maps { get; set; }
}