using Discord.Webhook;

namespace TMWRR.DiscordReport;

public static class Sample
{
    public static DiscordWebhookClient CreateWebhook(string url)
    {
        return new DiscordWebhookClient(url);
    }
}
