using System.ComponentModel.DataAnnotations;

namespace TMWRR.DiscordBot.Options
{
    public sealed class DiscordOptions
    {
        public const string Discord = "Discord";

        [Required]
        public required string Token { get; set; }

        public string TestGuildId { get; set; } = string.Empty;
    }
}
