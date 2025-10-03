using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using TMWRR.DiscordBot.Options;

namespace TMWRR.DiscordBot.Services
{
    public interface IDiscordBotService : IAsyncDisposable, IDisposable
    {
        Task StartAsync();
        Task StopAsync();

        Task<IThreadChannel?> CreateThreadAsync(ulong channelId, IMessage message, string name, CancellationToken cancellationToken = default);
        Task<IUser?> GetUserAsync(ulong userId, CancellationToken cancellationToken = default);
        Task<IMessage?> GetMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);
        Task<IUserMessage?> ModifyMessageAsync(ulong channelId, ulong messageId, Action<MessageProperties> func, CancellationToken cancellationToken = default);
        Task<RestApplication> GetInfoAsync(CancellationToken cancellationToken = default);
    }

    internal sealed class DiscordBotService : IDiscordBotService
    {
        private readonly IServiceProvider provider;
        private readonly DiscordSocketClient client;
        private readonly InteractionService interactionService;
        private readonly IOptions<DiscordOptions> options;
        private readonly IHostEnvironment env;
        private readonly ILogger<DiscordSocketClient> logger;

        public DiscordBotService(
            IServiceProvider provider,
            DiscordSocketClient client,
            InteractionService interactionService,
            IOptions<DiscordOptions> options,
            IHostEnvironment env,
            ILogger<DiscordSocketClient> logger)
        {
            this.env = env;
            this.provider = provider;
            this.client = client;
            this.interactionService = interactionService;
            this.options = options;
            this.logger = logger;
        }

        public async Task StartAsync()
        {
            logger.LogInformation("Starting bot...");
            logger.LogInformation("Preparing modules...");

            interactionService.Log += ClientLog;
            interactionService.InteractionExecuted += InteractionExecuted;

            await using var scope = provider.CreateAsyncScope();
            await interactionService.AddModulesAsync(typeof(Program).Assembly, scope.ServiceProvider);

            logger.LogInformation("Subscribing to events...");

            client.Log += ClientLog;
            client.InviteCreated += _ => Task.CompletedTask;
            client.GuildScheduledEventCreated += _ => Task.CompletedTask;
            client.Ready += ClientReady;
            client.InteractionCreated += async interaction =>
            {
                var context = new SocketInteractionContext(client, interaction);
                await interactionService.ExecuteCommandAsync(context, provider);
            };

            logger.LogInformation("Loggin in...");

            await client.LoginAsync(TokenType.Bot, options.Value.Token);

            logger.LogInformation("Starting...");

            await client.StartAsync();

            logger.LogInformation("Started!");
        }

        public async Task StopAsync()
        {
            await client.LogoutAsync();
            await client.StopAsync();
        }

        public async Task<IUserMessage?> SendMessageAsync(
            ulong channelId,
            string? message = null,
            Embed? embed = null,
            IEnumerable<FileAttachment>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            var options = new RequestOptions() { CancelToken = cancellationToken };

            var channel = await client.GetChannelAsync(channelId, options);

            if (channel is not IMessageChannel msgChannel)
            {
                return null;
            }

            if (attachments is not null && attachments.Any())
            {
                return await msgChannel.SendFilesAsync(attachments, message, embed: embed, options: options);
            }

            return await msgChannel.SendMessageAsync(message, embed: embed, options: options);
        }

        public async Task<IThreadChannel?> CreateThreadAsync(ulong channelId, IMessage message, string name, CancellationToken cancellationToken = default)
        {
            var channel = await client.GetChannelAsync(channelId);

            if (channel is not ITextChannel textChannel)
            {
                return null;
            }

            return await textChannel.CreateThreadAsync(name, message: message, options: new() { CancelToken = cancellationToken });
        }

        public async Task<IUser?> GetUserAsync(ulong userId, CancellationToken cancellationToken = default)
        {
            return await client.GetUserAsync(userId, new() { CancelToken = cancellationToken });
        }

        public async Task<IMessage?> GetMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default)
        {
            var options = new RequestOptions() { CancelToken = cancellationToken };

            var channel = await client.GetChannelAsync(channelId, options);

            if (channel is not IMessageChannel msgChannel)
            {
                return null;
            }

            return await msgChannel.GetMessageAsync(messageId, options: options);
        }

        public async Task<IUserMessage?> ModifyMessageAsync(ulong channelId, ulong messageId, Action<MessageProperties> func, CancellationToken cancellationToken = default)
        {
            var channel = await client.GetChannelAsync(channelId);

            if (channel is not IMessageChannel msgChannel)
            {
                return null;
            }

            return await msgChannel.ModifyMessageAsync(messageId, func, new() { CancelToken = cancellationToken });
        }

        public async Task<RestApplication> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            return await client.GetApplicationInfoAsync(new() { CancelToken = cancellationToken });
        }

        private async Task ClientReady()
        {
            // Does not need to be called every Ready event
            await RegisterCommandsAsync(deleteMissing: true);
        }

        private Task ClientLog(LogMessage msg)
        {
            logger.Log(msg.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Trace,
                _ => throw new NotImplementedException()
            },
                msg.Exception, "{message}", msg.Message ?? msg.Exception?.Message);

            return Task.CompletedTask;
        }

        private async Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, IResult result)
        {
            if (result.Error is null)
            {
                return;
            }

            switch (result)
            {
                case PreconditionResult precondition:
                    await context.Interaction.RespondAsync(precondition.ErrorReason, ephemeral: true);
                    break;
                case ExecuteResult executeResult:
                    var sb = new StringBuilder("` ");
                    sb.Append(executeResult.ErrorReason);
                    sb.Append(" ` (");
                    sb.Append(executeResult.Error);
                    sb.Append(')');

                    if (executeResult.Error is InteractionCommandError.Exception && executeResult.Exception.InnerException is not null)
                    {
                        var innerException = executeResult.Exception.InnerException;

                        sb.AppendLine();
                        sb.AppendLine("```");
                        sb.Append(innerException.GetType().Name);
                        sb.Append(": ");

                        if (!string.IsNullOrEmpty(innerException.Message))
                        {
                            if (innerException.Message.Length > 1000)
                            {
                                sb.Append(executeResult.Exception.InnerException.Message.AsSpan(0, 1000));
                                sb.Append("...");
                            }
                            else
                            {
                                sb.Append(executeResult.Exception.InnerException.Message);
                            }
                        }

                        sb.AppendLine("```");
                        sb.Append("The full error has been sent to the owner.");
                    }

                    var embed = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription(sb.ToString())
                        .WithColor(Color.Red)
                        .Build();

                    if (context.Interaction.HasResponded)
                    {
                        await context.Interaction.FollowupAsync(embed: embed, ephemeral: true);
                    }
                    else
                    {
                        await context.Interaction.RespondAsync(embed: embed, ephemeral: true);
                    }

                    break;
            }
        }

        private async Task RegisterCommandsAsync(bool deleteMissing = true)
        {
            logger.LogInformation("Registering commands...");

            if (env.IsDevelopment())
            {
                await interactionService.RegisterCommandsToGuildAsync(ulong.Parse(options.Value.TestGuildId), deleteMissing);
            }
            else
            {
                await interactionService.RegisterCommandsGloballyAsync(deleteMissing);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}