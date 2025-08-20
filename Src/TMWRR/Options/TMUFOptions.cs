namespace TMWRR.Options;

public sealed class TMUFOptions
{
    public TimeSpan CheckTimeOfDayCEST { get; set; }
    public TimeSpan CheckRetryTimeout { get; set; }
    public TimeSpan CheckRetryDelay { get; set; }
    public required string DiscordWebhookUrl { get; set; }
}
