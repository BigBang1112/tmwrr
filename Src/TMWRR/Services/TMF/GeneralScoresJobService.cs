using ManiaAPI.Xml.TMUF;
using TMWRR.Data;

namespace TMWRR.Services.TMF;

public interface IGeneralScoresJobService
{
    Task ProcessAsync(Leaderboard leaderboard, CancellationToken cancellationToken);
}

public class GeneralScoresJobService : IGeneralScoresJobService
{
    private readonly AppDbContext db;

    public GeneralScoresJobService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task ProcessAsync(Leaderboard leaderboard, CancellationToken cancellationToken)
    {
        var playerCount = leaderboard.Skillpoints.Sum(x => x.Count);
    }
}
