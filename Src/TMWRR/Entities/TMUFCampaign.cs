using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public class TMUFCampaign
{
    [StringLength(32)]
    public string Id { get; set; } = string.Empty;

    public ICollection<TMUFScoresSnapshot> ScoresSnapshots { get; set; } = [];
}
