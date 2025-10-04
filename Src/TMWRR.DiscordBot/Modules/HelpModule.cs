using Discord.Interactions;

namespace TMWRR.DiscordBot.Modules;

public sealed class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("2help", "How to use TMWR v2?")]
    public async Task Help()
    {
        await RespondAsync("Running via https://api.tmwrr.bigbang1112.cz/");
    }
}