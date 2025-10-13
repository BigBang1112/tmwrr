namespace TMWRR.DiscordReport;

public interface IDiscordWebhookFactory
{
    IDiscordWebhook Create(string webhookUrl);
}

public sealed class DiscordWebhookFactory : IDiscordWebhookFactory
{
    public IDiscordWebhook Create(string webhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);

        return DiscordWebhook.Create(webhookUrl);
    }
}
