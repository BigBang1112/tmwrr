namespace TMWRR.Extensions;

internal static class HttpResponseExtensions
{
    public static void ClientCache(this HttpResponse response)
    {
        response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";
        response.GetTypedHeaders().LastModified = DateTimeOffset.UtcNow;
    }
}
