using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities.TMF;

[Index(nameof(CreatedAt), IsUnique = true)]
public class TMFCampaignScoresSnapshot
{
    public int Id { get; set; }

    [Required]
    public TMFCampaign Campaign { get; set; } = null!;
    public string CampaignId { get; set; } = string.Empty;

    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset PublishedAt { get; set; }
    //public string? Etag { get; set; } not returned from ManiaAPI

    public bool NoChanges { get; set; }

    public ICollection<TMFCampaignScoresRecord> Records { get; set; } = [];

    public override string ToString()
    {
        return $"{CampaignId} - {CreatedAt:yyyy-MM-dd HH:mm:ss} ({Records.Count} records)";
    }
}
