using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMWRR.Entities.TMF;

namespace TMWRR.Entities;

[Index(nameof(Guid), IsUnique = true)]
public class Ghost
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [Column(TypeName = "mediumblob")]
    public required byte[] Data { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    [StringLength(64)]
    public string? Etag { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Url { get; set; }

    public ICollection<TMFCampaignScoresRecord> Records { get; set; } = [];
}
