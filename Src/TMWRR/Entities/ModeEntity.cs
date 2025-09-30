using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public sealed class ModeEntity
{
    [StringLength(16)]
    public string Id { get; set; } = string.Empty;

    public ICollection<MapEntity> Maps { get; set; } = [];
}
