using System.ComponentModel.DataAnnotations;

namespace TMWRR.Options;

public sealed class TMUFWebServices
{
    [Required]
    public required string ApiUsername { get; set; }

    [Required]
    public required string ApiPassword { get; set; }
}
