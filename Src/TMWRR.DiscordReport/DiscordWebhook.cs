using Discord.Webhook;

namespace TMWRR.DiscordReport;

public interface IDiscordWebhook : IDisposable
{
    Task SendMessageAsync(string text, CancellationToken cancellationToken);
}

internal sealed class DiscordWebhook : IDiscordWebhook
{
    private readonly DiscordWebhookClient client;

    private DiscordWebhook(DiscordWebhookClient client)
    {
        this.client = client;
    }

    public async Task SendMessageAsync(string text, CancellationToken cancellationToken)
    {
        var embed = new Discord.EmbedBuilder()
            .WithDescription(text)
            .WithColor(Discord.Color.Blue)
            .WithFooter("TMWRR (TMUF Solo Changes) Experimental")
            .Build();

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
