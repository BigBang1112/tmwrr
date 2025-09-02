using System.ComponentModel.DataAnnotations;

namespace TMWRR.Options;

public sealed class TMUFDiscord
{
    [Required]
    public required string TestWebhookUrl { get; set; }
}
