using System.ComponentModel.DataAnnotations;

namespace TMWRR.Options;

public sealed class TMUFOptions
{
    public bool EnableSoloReport { get; set; } = true;
    public bool EnableLoginDetails { get; set; } = true;
    public bool EnableGhostDownload { get; set; } = true;

    public TimeSpan CheckTimeOfDayCEST { get; set; }
    public TimeSpan CheckRetryTimeout { get; set; }
    public TimeSpan CheckRetryDelay { get; set; }

    [Required]
    public required string DiscordWebhookUrl { get; set; }

    [Required]
    public required string ChangesDiscordWebhookUrl { get; set; }

    [Required]
    public required string ApiUsername { get; set; }

    [Required]
    public required string ApiPassword { get; set; }
}
