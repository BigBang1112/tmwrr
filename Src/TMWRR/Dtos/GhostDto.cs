namespace TMWRR.Dtos;

public sealed class GhostDto
{
    public required Guid Guid { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
