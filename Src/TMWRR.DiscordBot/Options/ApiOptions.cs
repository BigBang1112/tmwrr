using System.ComponentModel.DataAnnotations;

namespace TMWRR.DiscordBot.Options;

public sealed class ApiOptions
{
    public const string API = "API";

    [Required]
    public required string BaseAddress { get; set; }

    [Required]
    public required string PublicBaseAddress { get; set; }
}
