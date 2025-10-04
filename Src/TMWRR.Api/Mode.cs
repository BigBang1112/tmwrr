namespace TMWRR.Api;

public sealed class Mode
{
    public required string Id { get; set; }

    public EMode GetEnum()
    {
        return Enum.Parse<EMode>(Id);
    }

    public bool IsStunts()
    {
        return Id == nameof(EMode.Stunts);
    }

    public bool IsPlatform()
    {
        return Id == nameof(EMode.Platform);
    }

    public bool IsPuzzle()
    {
        return Id == nameof(EMode.Puzzle);
    }
}
