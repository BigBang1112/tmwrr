using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public class TMFCampaign
{
    [StringLength(32)]
    public string Id { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Name { get; set; }

    public ICollection<TMFCampaignScoresSnapshot> ScoresSnapshots { get; set; } = [];

    public override string ToString()
    {
        return Id;
    }
}
