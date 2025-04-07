using Discord.Webhook;

namespace TMWRR.DiscordReport;

public static class Sample
{
    public static async Task ReportAsync(string text)
    {
        var webhook = new DiscordWebhookClient("");
        await webhook.SendMessageAsync(text);
    }
}
