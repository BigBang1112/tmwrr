using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Entities;

namespace TMWRR.Services;

public interface IGhostService
{
    Task<Ghost?> CreateGhostAsync(Map map, TMFLogin login, CancellationToken cancellationToken);
    Task<GhostDataDto?> GetGhostDataAsync(Guid guid, CancellationToken cancellationToken);
}

public sealed class GhostService : IGhostService
{
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<GhostService> logger;

    public GhostService(AppDbContext db, HttpClient http, ILogger<GhostService> logger)
    {
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<Ghost?> CreateGhostAsync(Map map, TMFLogin login, CancellationToken cancellationToken)
    {
        if (map.TMFCampaign is null || login.RegistrationId is null)
        {
            logger.LogWarning("Cannot download ghost for map {MapUid} and login {Login}, probably missing registration ID", map.MapUid, login.Id);
            return null;
        }

        var url = $"http://data.trackmaniaforever.com/official_replays/records/{map.TMFCampaign.Section}/{map.TMFCampaign.StartId + map.Order}/{login.RegistrationId}.replay.gbx";

        using var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to download ghost for map {MapUid} and login {Login}, status code {StatusCode}", map.MapUid, login.Id, response.StatusCode);
            return null;
        }

        return new Ghost
        {
            Data = await response.Content.ReadAsByteArrayAsync(cancellationToken),
            LastModifiedAt = response.Content.Headers.LastModified,
            Etag = response.Headers.ETag?.Tag,
            Url = url,
        };
    }

    public async Task<GhostDataDto?> GetGhostDataAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.Ghosts
            .Where(x => x.Guid == guid)
            .Select(x => new GhostDataDto
            {
                Data = x.Data,
                LastModifiedAt = x.LastModifiedAt,
                Etag = x.Etag
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
