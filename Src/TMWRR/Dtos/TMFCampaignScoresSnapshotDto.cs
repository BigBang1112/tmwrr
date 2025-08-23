namespace TMWRR.Dtos;

public sealed class TMFCampaignScoresSnapshotDto
{
    public required TMFCampaignDto Campaign { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset PublishedAt { get; set; }
    public bool NoChanges { get; set; }
}
