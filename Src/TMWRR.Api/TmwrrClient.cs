using System.Net;
using System.Net.Http.Json;
using System.Text;
using TMWRR.Api.TMF;

namespace TMWRR.Api;

public sealed class TmwrrClient
{
    private readonly HttpClient client;

    public TmwrrClient(HttpClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.client.DefaultRequestHeaders.UserAgent.ParseAdd("TMWRR.Api/1.0 (Discord=bigbang1112)");
    }

    public TmwrrClient(string baseAddress = "https://api.tmwrr.bigbang1112.cz") : this(new HttpClient { BaseAddress = new Uri(baseAddress) })
    {

    }

    public async Task<TmwrrInformation> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("", cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(TmwrrJsonSerializerContext.Default.TmwrrInformation, cancellationToken) ?? throw new InvalidOperationException("Response content is null");
    }

    public async Task<IEnumerable<Map>> GetMapsAsync(string name = "", int length = 25, int offset = 0, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder("/maps");
        var hasQuery = false;

        if (!string.IsNullOrWhiteSpace(name))
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("name=").Append(Uri.EscapeDataString(name));
            hasQuery = true;
        }

        if (length > 0)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("length=").Append(length);
            hasQuery = true;
        }

        if (offset > 0)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("offset=").Append(offset);
        }

        using var response = await client.GetAsync(sb.ToString(), cancellationToken);

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync(TmwrrJsonSerializerContext.Default.IEnumerableMap, cancellationToken) ?? [];
    }

    public async Task<Map?> GetMapAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mapUid))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(mapUid));
        }

        using var response = await client.GetAsync($"/maps/{Uri.EscapeDataString(mapUid)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync(TmwrrJsonSerializerContext.Default.Map, cancellationToken);
    }

    public async Task<TMFCampaignScoresSnapshot?> GetLatestTMFCampaignSnapshotAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(campaignId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(campaignId));
        }

        using var response = await client.GetAsync($"/games/TMF/campaigns/{Uri.EscapeDataString(campaignId)}/snapshots/latest", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync(TmwrrJsonSerializerContext.Default.TMFCampaignScoresSnapshot, cancellationToken)!;
    }

    public async Task<TMFCampaignScoresSnapshot?> GetLatestTMFCampaignSnapshotAsync(string campaignId, string mapUid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(campaignId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(campaignId));
        }

        using var response = await client.GetAsync($"/games/TMF/campaigns/{Uri.EscapeDataString(campaignId)}/snapshots/latest/{mapUid}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync(TmwrrJsonSerializerContext.Default.TMFCampaignScoresSnapshot, cancellationToken)!;
    }

    public static string GetMapThumbnailEndpoint(string mapUid)
    {
        if (string.IsNullOrWhiteSpace(mapUid))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(mapUid));
        }

        return $"maps/{Uri.EscapeDataString(mapUid)}/thumbnail";
    }

    public Uri GetMapThumbnailUrl(string mapUid)
    {
        return new Uri(client.BaseAddress ?? throw new InvalidOperationException("Base address is not set"), GetMapThumbnailEndpoint(mapUid));
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (response.StatusCode == HttpStatusCode.BadRequest)
        {
            ProblemDetails problemDetails;
            try
            {
                problemDetails = (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken))!;
            }
            catch
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new TmwrrProblemException(content, ex);
            }

            throw new TmwrrProblemException(problemDetails, ex);
        }
        catch (HttpRequestException ex) when (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new TmwrrRateLimitException("Rate limit exceeded", ex);
        }
    }
}