namespace TMWRR.Models.Resources;

public sealed record MapTMFResource(
    string Name,
    string AuthorLogin,
    string Environment,
    string Mode,
    int? AuthorTime,
    int? AuthorScore,
    int NbLaps,
    string? Campaign,
    int? Order,
    string FileName);
