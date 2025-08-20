using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

[Index(nameof(CreatedAt), IsUnique = true)]
public class TMUFCampaignScoresSnapshot
{
    public int Id { get; set; }

    [Required]
    public TMUFCampaign Campaign { get; set; } = null!;
    public string CampaignId { get; set; } = string.Empty;

    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset PublishedAt { get; set; }
}
