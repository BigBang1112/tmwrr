using System.Collections.Immutable;
using TmEssentials;
using TMWRR.Api.TMF;

namespace TMWRR.Api;

public sealed class Map
{
    public required string MapUid { get; set; }
    public string? Name { get; set; }
    public string? DeformattedName { get; set; }
    public User? Author { get; set; }
    public TMEnvironment? Environment { get; set; }
    public Mode? Mode { get; set; }
    public TimeInt32? AuthorTime { get; set; }
    public int? AuthorScore { get; set; }
    public int? NbLaps { get; set; }
    public TMFCampaign? CampaignTMF { get; set; }
    public int? RecordCountTMF { get; set; }
    public ImmutableList<TMFCampaignScoresRecord>? RecordsTMF { get; set; }
    public int? Order { get; set; }
    public string? FileName { get; set; }
}
