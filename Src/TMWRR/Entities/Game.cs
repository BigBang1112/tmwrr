using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public sealed class Game
{
    [StringLength(12)]
    public string Id { get; set; } = string.Empty;
}
