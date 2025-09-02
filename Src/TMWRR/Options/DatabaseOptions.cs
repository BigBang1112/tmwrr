namespace TMWRR.Options;

public sealed class DatabaseOptions
{
    public bool EnableSeeding { get; set; } = true;
    public bool FillMissingGhostInfo { get; set; } = true;
}
