namespace TMWRR.Dtos;

public sealed class TMFReplayDto
{
    public required Guid Guid { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
