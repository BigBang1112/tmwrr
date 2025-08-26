using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Enums;

namespace TMWRR.Services;

public interface IGameService
{
    Task<IEnumerable<GameDto>> GetAllDtosAsync(CancellationToken cancellationToken);
    Task<GameDto?> GetDtoAsync(EGame id, CancellationToken cancellationToken);
}

public sealed class GameService : IGameService
{
    private readonly AppDbContext db;

    public GameService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IEnumerable<GameDto>> GetAllDtosAsync(CancellationToken cancellationToken)
    {
        return await db.Games
            .Include(x => x.Environments)
            .Select(x => new GameDto
            {
                Id = x.Id,
                Environments = x.Environments.Select(e => new TMEnvironmentDto
                {
                    Id = e.Id,
                    Name = e.Name ?? e.Id
                }).ToImmutableList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GameDto?> GetDtoAsync(EGame id, CancellationToken cancellationToken)
    {
        return await db.Games
            .Include(x => x.Environments)
            .Select(x => new GameDto
            {
                Id = x.Id,
                Environments = x.Environments.Select(e => new TMEnvironmentDto
                {
                    Id = e.Id,
                    Name = e.Name ?? e.Id
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(x => x.Id == id.ToString(), cancellationToken);
    }
}
