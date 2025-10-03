using System.Text.Json.Serialization;
using TMWRR.DiscordBot.Models;

namespace TMWRR.DiscordBot
{
    [JsonSerializable(typeof(User))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    internal partial class GitHubJsonSerializerContext : JsonSerializerContext;
}