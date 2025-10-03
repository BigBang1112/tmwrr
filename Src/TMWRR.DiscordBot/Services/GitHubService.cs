using System.Net;
using System.Net.Http.Json;
using TMWRR.DiscordBot.Models;

namespace TMWRR.DiscordBot.Services
{
    public interface IGitHubService
    {
        Task<User?> GetUserAsync(string login, CancellationToken cancellationToken = default);
    }

    public sealed class GitHubService : IGitHubService
    {
        private readonly HttpClient http;

        public GitHubService(HttpClient http)
        {
            this.http = http;
        }

        public async Task<User?> GetUserAsync(string login, CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync($"https://api.github.com/users/{login}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var nice = await response.Content.ReadAsStringAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync(GitHubJsonSerializerContext.Default.User, cancellationToken);

            return user;
        }
    }
}
