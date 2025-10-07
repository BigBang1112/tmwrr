namespace TMWRR.Entities.TMF;

public class TMFCampaignScoresPlayerCountEntity
{
    public int Id { get; set; }
    public required TMFCampaignScoresSnapshotEntity Snapshot { get; set; }
    public required MapEntity Map { get; set; }
    public required int Count { get; set; }
    public required int? DnfCount { get; set; }
}
