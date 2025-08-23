using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TmEssentials;
using TMWRR.Enums;

namespace TMWRR.Entities;

[Index(nameof(MapUid))]
public class Map
{
    public int Id { get; set; }

    [StringLength(32)]
    public required string MapUid { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Name { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DeformattedName { get; set; }

    public User? Author { get; set; }

    public TMEnvironment? Environment { get; set; }
    public string? EnvironmentId { get; set; }

    public Mode? Mode { get; set; }
    public string? ModeId { get; set; }

    public TimeInt32? AuthorTime { get; set; }

    public int? AuthorScore { get; set; }

    public int NbLaps { get; set; } = 1;

    public TMFCampaign? TMFCampaign { get; set; }
    public string? TMFCampaignId { get; set; }

    public int? Order { get; set; }

    [StringLength(byte.MaxValue)]
    public string? FileName { get; set; }

    [MaxLength(512_000)]
    public byte[]? Thumbnail { get; set; }

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
}
