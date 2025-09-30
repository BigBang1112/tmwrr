namespace TMWRR.Api;

public sealed class TMEnvironment
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public Game? Game { get; set; }

    //public ICollection<MapDto> Maps { get; set; } = [];
}
