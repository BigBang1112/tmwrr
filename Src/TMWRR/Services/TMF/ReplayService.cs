using TMWRR.Data;
using TMWRR.Entities;

namespace TMWRR.Services.TMF;

public interface IReplayService
{
    Task<TMFReplay?> CreateReplayAsync(Map map, TMFLogin login, CancellationToken cancellationToken);
}

public sealed class ReplayService : IReplayService
{
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<ReplayService> logger;

    public ReplayService(AppDbContext db, HttpClient http, ILogger<ReplayService> logger)
    {
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<TMFReplay?> CreateReplayAsync(Map map, TMFLogin login, CancellationToken cancellationToken)
    {
        if (map.TMFCampaign is null || login.RegistrationId is null)
        {
            logger.LogWarning("Cannot download replay for map {MapUid} and login {Login}, probably missing registration ID", map.MapUid, login.Id);
            return null;
        }

        var url = $"http://data.trackmaniaforever.com/official_replays/records/{map.TMFCampaign.Section}/{map.TMFCampaign.StartId + map.Order}/{login.RegistrationId}.replay.gbx";

        using var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to download replay for map {MapUid} and login {Login}, status code {StatusCode}", map.MapUid, login.Id, response.StatusCode);
            return null;
        }

        return new TMFReplay
        {
            Data = await response.Content.ReadAsByteArrayAsync(cancellationToken),
            LastModifiedAt = response.Content.Headers.LastModified,
            Etag = response.Headers.ETag?.Tag,
            Url = url,
        };
    }
}
