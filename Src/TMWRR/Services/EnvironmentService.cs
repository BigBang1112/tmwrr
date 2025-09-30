using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Api;

namespace TMWRR.Services;

public interface IEnvironmentService
{
    Task<IEnumerable<TMEnvironment>> GetAllDtosAsync(CancellationToken cancellationToken);
    Task<TMEnvironment?> GetDtoAsync(string id, CancellationToken cancellationToken);
}

public sealed class EnvironmentService : IEnvironmentService
{
    private readonly AppDbContext db;

    public EnvironmentService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IEnumerable<TMEnvironment>> GetAllDtosAsync(CancellationToken cancellationToken)
    {
        return await db.Environments
            .Select(x => new TMEnvironment
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
                Game = new Game
                {
                    Id = x.Game.Id
                }
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TMEnvironment?> GetDtoAsync(string id, CancellationToken cancellationToken)
    {
        return await db.Environments
            .Select(x => new TMEnvironment
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
                Game = new Game
                {
                    Id = x.Game.Id
                }
            })
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
