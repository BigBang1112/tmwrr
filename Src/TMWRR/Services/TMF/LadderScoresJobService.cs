using ManiaAPI.Xml.TMUF;
using TMWRR.Data;

namespace TMWRR.Services.TMF;

public interface ILadderScoresJobService
{
    Task ProcessAsync(LadderZone ladder, CancellationToken cancellationToken);
}

public class LadderScoresJobService : ILadderScoresJobService
{
    private readonly AppDbContext db;

    public LadderScoresJobService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task ProcessAsync(LadderZone ladder, CancellationToken cancellationToken)
    {
        
    }
}
