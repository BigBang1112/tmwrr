namespace TMWRR.Dtos;

public sealed class TMEnvironmentDto
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public GameDto? Game { get; set; }

    //public ICollection<MapDto> Maps { get; set; } = [];
}
