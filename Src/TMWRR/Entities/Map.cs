using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

[Index(nameof(MapUid))]
public class Map
{
    public int Id { get; set; }

    [StringLength(32)]
    public required string MapUid { get; set; }

    public override string ToString()
    {
        return MapUid;
    }
}
