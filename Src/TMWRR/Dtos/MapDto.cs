using TmEssentials;

namespace TMWRR.Dtos;

public sealed class MapDto
{
    public required string MapUid { get; set; }
    public string? Name { get; set; }
    public string? DeformattedName { get; set; }
    public UserDto? Author { get; set; }
    public TMEnvironmentDto? Environment { get; set; }
    public ModeDto? Mode { get; set; }
    public TimeInt32? AuthorTime { get; set; }
    public int? AuthorScore { get; set; }
    public int? NbLaps { get; set; }
    public TMFCampaignDto? CampaignTMF { get; set; }
    public int? Order { get; set; }
    public string? FileName { get; set; }
}
