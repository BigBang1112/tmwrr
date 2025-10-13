using System.ComponentModel.DataAnnotations;

namespace TMWRR.Options;

public sealed class TM2Options
{
    [Required]
    public required TM2Discord Discord { get; set; }
}
