namespace TMWRR.Api;

public sealed class DownloadContent
{
    public required byte[] Data { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? Etag { get; set; }
}
