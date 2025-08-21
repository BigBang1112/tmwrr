using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

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
}
