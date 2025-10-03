namespace TMWRR.DiscordBot.Models
{
    public sealed class User
    {
        public required string Login { get; set; }
        public required int Followers { get; set; }
    }
}
