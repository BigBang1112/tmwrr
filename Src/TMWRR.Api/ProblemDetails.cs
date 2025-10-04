namespace TMWRR.Api;

public record ProblemDetails
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public Dictionary<string, string[]> Errors { get; init; } = [];
}