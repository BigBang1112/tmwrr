using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities.TMF;

public class TMFCampaignEntity
{
    [StringLength(32)]
    public string Id { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Name { get; set; }

    [StringLength(13)]
    public required string Section { get; set; }

    public required int StartId { get; set; }

    public ICollection<TMFCampaignScoresSnapshotEntity> ScoresSnapshots { get; set; } = [];
    public ICollection<MapEntity> Maps { get; set; } = [];

    public override string ToString()
    {
        return Id;
    }
}
