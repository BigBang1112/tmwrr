using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public class TMEnvironmentEntity
{
    [StringLength(16)]
    public string Id { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Name { get; set; }

    [Required]
    public GameEntity Game { get; set; } = default!;
    public string GameId { get; set; } = string.Empty;

    public ICollection<MapEntity> Maps { get; set; } = [];
}
