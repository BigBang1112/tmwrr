using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TmEssentials;
using TMWRR.Api;
using TMWRR.Entities.TMF;

namespace TMWRR.Entities;

[Index(nameof(MapUid))]
public class MapEntity
{
    public int Id { get; set; }

    [StringLength(32)]
    public required string MapUid { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Name { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DeformattedName { get; set; }

    public UserEntity? Author { get; set; }

    public TMEnvironmentEntity? Environment { get; set; }
    public string? EnvironmentId { get; set; }

    public ModeEntity? Mode { get; set; }
    public string? ModeId { get; set; }

    public TimeInt32? AuthorTime { get; set; }

    public int? AuthorScore { get; set; }

    public int NbLaps { get; set; } = 1;

    public TMFCampaignEntity? TMFCampaign { get; set; }
    public string? TMFCampaignId { get; set; }

    public int? Order { get; set; }

    [StringLength(byte.MaxValue)]
    public string? FileName { get; set; }

    [Column(TypeName = "mediumblob")]
    public byte[]? Thumbnail { get; set; }

    public ICollection<TMFCampaignScoresRecordEntity> TMFRecords { get; set; } = [];
    public ICollection<TMFCampaignScoresPlayerCountEntity> TMFPlayerCounts { get; set; } = [];

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return MapUid;
        }

        return $"{MapUid} ({Name})";
    }

    public string GetDeformattedName()
    {
        if (!string.IsNullOrEmpty(DeformattedName))
        {
            return DeformattedName;
        }

        if (!string.IsNullOrEmpty(Name))
        {
            return TextFormatter.Deformat(Name);
        }

        return MapUid;
    }

    public EMode? GetMode()
    {
        if (string.IsNullOrEmpty(ModeId))
        {
            return null;
        }

        return Enum.Parse<EMode>(ModeId);
    }

    public bool IsStunts()
    {
        return ModeId == nameof(EMode.Stunts);
    }

    public bool IsPlatform()
    {
        return ModeId == nameof(EMode.Platform);
    }

    public bool IsPuzzle()
    {
        return ModeId == nameof(EMode.Puzzle);
    }
}
