using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Entities;

namespace TMWRR.Services;

public interface ILoginService
{
    ValueTask<IDictionary<string, TMFLogin>> PopulateAsync(IDictionary<string, string> loginNicknameDict, CancellationToken cancellationToken);
    //Task<TMFLogin?> GetTMFAsync(string login, CancellationToken cancellationToken);
    Task<TMFLoginDto?> GetTMFDtoAsync(string login, CancellationToken cancellationToken);
    ValueTask<IEnumerable<TMFLogin>> GetMultipleTMFAsync(IEnumerable<string> logins, CancellationToken cancellationToken);
    //ValueTask<IReadOnlyDictionary<string, string?>> GetMultipleNicknamesAsync(IEnumerable<string> logins, CancellationToken cancellationToken);
}

public sealed class LoginService : ILoginService
{
    private readonly AppDbContext db;

    public LoginService(AppDbContext db)
    {
        this.db = db;
    }

    public async ValueTask<IDictionary<string, TMFLogin>> PopulateAsync(IDictionary<string, string> loginNicknameDict, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loginNicknameDict, nameof(loginNicknameDict));

        if (loginNicknameDict.Count == 0)
        {
            return new Dictionary<string, TMFLogin>();
        }

        var logins = await db.TMFLogins
            .Where(e => loginNicknameDict.Keys.Contains(e.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var login in logins.Values)
        {
            // TODO: NicknameHistory
            login.Nickname = loginNicknameDict[login.Id];
        }

        var missingLogins = loginNicknameDict.Keys.Except(logins.Keys).Select(x => new TMFLogin
        {
            Id = x,
            Nickname = loginNicknameDict[x],
        }).ToList();

        if (missingLogins.Count == 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            return logins;
        }

        await db.TMFLogins.AddRangeAsync(missingLogins, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var login in missingLogins)
        {
            logins[login.Id] = login;
        }

        return logins;
    }

    public async Task<TMFLogin?> GetTMFAsync(string login, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(login, nameof(login));

        return await db.TMFLogins.FirstOrDefaultAsync(x => x.Id == login, cancellationToken);
    }

    public async Task<TMFLoginDto?> GetTMFDtoAsync(string login, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(login, nameof(login));

        return await db.TMFLogins
            .Include(x => x.Users)
            .Select(x => new TMFLoginDto
            {
                Id = x.Id,
                Nickname = x.Nickname,
                Users = x.Users.Select(u => new UserDto
                {
                    Guid = u.Guid
                }).ToImmutableList(),
            })
            .FirstOrDefaultAsync(x => x.Id == login, cancellationToken);
    }

    public async ValueTask<IEnumerable<TMFLogin>> GetMultipleTMFAsync(IEnumerable<string> logins, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logins, nameof(logins));

        if (!logins.Any())
        {
            return [];
        }

        return await db.TMFLogins
            .Where(x => logins.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyDictionary<string, string?>> GetMultipleNicknamesAsync(IEnumerable<string> logins, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logins, nameof(logins));

        if (!logins.Any())
        {
            return new Dictionary<string, string?>();
        }

        return await db.TMFLogins
            .Where(x => logins.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Nickname, cancellationToken);
    }
}
