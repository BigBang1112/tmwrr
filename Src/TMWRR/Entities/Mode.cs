using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public sealed class Mode
{
    [StringLength(16)]
    public string Id { get; set; } = string.Empty;

    public ICollection<Map> Maps { get; set; } = [];
}
