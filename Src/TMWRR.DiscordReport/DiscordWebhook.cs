using Discord;
using Discord.Webhook;

namespace TMWRR.DiscordReport;

public interface IDiscordWebhook : IDisposable
{
    Task SendMessageAsync(Embed embed, CancellationToken cancellationToken);
}

internal sealed class DiscordWebhook : IDiscordWebhook
{
    private readonly DiscordWebhookClient client;

    private DiscordWebhook(DiscordWebhookClient client)
    {
        this.client = client;
    }

    public async Task SendMessageAsync(Embed embed, CancellationToken cancellationToken)
    {
        await client.SendMessageAsync(embeds: [embed], options: new() { CancelToken = cancellationToken });
    }

    public static DiscordWebhook Create(string url)
    {
        var client = new DiscordWebhookClient(url);
        return new DiscordWebhook(client);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
