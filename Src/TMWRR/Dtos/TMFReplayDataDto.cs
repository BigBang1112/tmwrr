namespace TMWRR.Dtos;

public sealed class TMFReplayDataDto
{
    public required byte[] Data { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? Etag { get; set; }
}
