using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public class TMFReplay
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [MaxLength(2_000_000)]
    public required byte[] Data { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    [StringLength(64)]
    public string? Etag { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Url { get; set; }

    public ICollection<TMFCampaignScoresRecord> Records { get; set; } = [];
}
