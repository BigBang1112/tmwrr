using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Dtos;

namespace TMWRR.Services;

public interface IEnvironmentService
{
    Task<IEnumerable<TMEnvironmentDto>> GetAllDtosAsync(CancellationToken cancellationToken);
    Task<TMEnvironmentDto?> GetDtoAsync(string id, CancellationToken cancellationToken);
}

public sealed class EnvironmentService : IEnvironmentService
{
    private readonly AppDbContext db;

    public EnvironmentService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IEnumerable<TMEnvironmentDto>> GetAllDtosAsync(CancellationToken cancellationToken)
    {
        return await db.Environments
            .Select(x => new TMEnvironmentDto
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
                Game = new GameDto
                {
                    Id = x.Game.Id
                }
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TMEnvironmentDto?> GetDtoAsync(string id, CancellationToken cancellationToken)
    {
        return await db.Environments
            .Select(x => new TMEnvironmentDto
            {
                Id = x.Id,
                Name = x.Name ?? x.Id,
                Game = new GameDto
                {
                    Id = x.Game.Id
                }
            })
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
