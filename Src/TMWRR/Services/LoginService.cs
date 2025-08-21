using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Entities;

namespace TMWRR.Services;

public interface ILoginService
{
    ValueTask<IDictionary<string, TMFLogin>> PopulateAsync(IDictionary<string, string> loginNicknameDict, CancellationToken cancellationToken);
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
}
