using System.ComponentModel.DataAnnotations;

namespace TMWRR.Options;

public sealed class TM2Discord
{
    [Required]
    public required string TestWebhookUrl { get; set; }
}
