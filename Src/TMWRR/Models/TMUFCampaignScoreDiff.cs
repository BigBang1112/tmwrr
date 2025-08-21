namespace TMWRR.Models;

public class TMUFCampaignScoreDiff
{
    public List<TMUFCampaignScore> NewRecords { get; } = [];
    public List<(TMUFCampaignScore Old, TMUFCampaignScore New)> ImprovedRecords { get; } = [];
    public List<TMUFCampaignScore> RemovedRecords { get; } = [];
    public List<(TMUFCampaignScore Old, TMUFCampaignScore New)> WorsenedRecords { get; } = [];

    public bool IsEmpty => 
        NewRecords.Count == 0 && 
        ImprovedRecords.Count == 0 && 
        RemovedRecords.Count == 0 && 
        WorsenedRecords.Count == 0;
}
