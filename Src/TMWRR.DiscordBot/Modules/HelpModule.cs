using Discord.Interactions;

namespace TMWRR.DiscordBot.Modules
{
    public sealed class HelpModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("help", "How to use this?")]
        public async Task Help()
        {
            await RespondAsync("This bot is designed to help you with various tasks.");
        }
    }
}