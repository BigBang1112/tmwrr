namespace TMWRR.Api.TMF;

public sealed class TMFCampaignScoresSnapshot
{
    public required TMFCampaign Campaign { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset PublishedAt { get; set; }
    public bool NoChanges { get; set; }
}
