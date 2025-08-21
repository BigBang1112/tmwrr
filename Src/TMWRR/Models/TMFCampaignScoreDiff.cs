namespace TMWRR.Models;

public class TMFCampaignScoreDiff
{
    public List<TMFCampaignScore> NewRecords { get; } = [];
    public List<(TMFCampaignScore Old, TMFCampaignScore New)> ImprovedRecords { get; } = [];
    public List<TMFCampaignScore> RemovedRecords { get; } = [];
    public List<(TMFCampaignScore Old, TMFCampaignScore New)> WorsenedRecords { get; } = [];

    public bool IsEmpty => 
        NewRecords.Count == 0 && 
        ImprovedRecords.Count == 0 && 
        RemovedRecords.Count == 0 && 
        WorsenedRecords.Count == 0;
}
