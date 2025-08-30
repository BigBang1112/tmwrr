using ManiaAPI.TrackmaniaWS;
using ManiaAPI.UnitedLadder;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Net;
using TmEssentials;
using TMWRR.Data;
using TMWRR.Dtos;
using TMWRR.Dtos.TMF;
using TMWRR.Entities.TMF;

namespace TMWRR.Services;

public interface ILoginService
{
    ValueTask<IDictionary<string, TMFLogin>> PopulateAsync(IDictionary<string, string> loginNicknameDict, bool enableDetails, CancellationToken cancellationToken);
    //Task<TMFLogin?> GetTMFAsync(string login, CancellationToken cancellationToken);
    Task<TMFLoginDto?> GetTMFDtoAsync(string login, CancellationToken cancellationToken);
    ValueTask<IEnumerable<TMFLogin>> GetMultipleTMFAsync(IEnumerable<string> logins, CancellationToken cancellationToken);
    //ValueTask<IReadOnlyDictionary<string, string?>> GetMultipleNicknamesAsync(IEnumerable<string> logins, CancellationToken cancellationToken);
}

public sealed class LoginService : ILoginService
{
    private readonly AppDbContext db;
    private readonly TrackmaniaWS ws;
    private readonly UnitedLadder ul;
    private readonly ILogger<LoginService> logger;

    public LoginService(AppDbContext db, TrackmaniaWS ws, UnitedLadder ul, ILogger<LoginService> logger)
    {
        this.db = db;
        this.ws = ws;
        this.ul = ul;
        this.logger = logger;
    }

    public async ValueTask<IDictionary<string, TMFLogin>> PopulateAsync(IDictionary<string, string> loginNicknameDict, bool enableDetails, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loginNicknameDict);

        if (loginNicknameDict.Count == 0)
        {
            return new Dictionary<string, TMFLogin>();
        }

        var logins = await db.TMFLogins
            .Where(e => loginNicknameDict.Keys.Contains(e.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var rateLimited = false;
        var deadend = false;

        foreach (var login in logins.Values)
        {
            // TODO: NicknameHistory
            var nickname = loginNicknameDict[login.Id];
            login.Nickname = nickname;
            login.NicknameDeformatted = TextFormatter.Deformat(nickname);

            if (deadend || login.RegistrationId is not null || !enableDetails)
            {
                continue;
            }

            RateLimitWhenUpdating:

            if (rateLimited)
            {
                try
                {
                    var player = await ul.GetPlayerAsync(login.Id, cancellationToken);
                    login.RegistrationId = player.Id;
                }
                catch (Exception ex)
                {
                    deadend = true;
                    logger.LogWarning(ex, "UnitedLadder lookup failed while updating TMF login {Login}, will not try again...", login.Id);
                }
            }
            else
            {
                try
                {
                    var player = await ws.GetPlayerAsync(login.Id, cancellationToken);
                    login.RegistrationId = player.Id;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    rateLimited = true;
                    logger.LogWarning("Rate limited reached while updating TMF login {Login}, falling back to UnitedLadder API...", login.Id);
                    goto RateLimitWhenUpdating;
                }
            }
        }

        var missingLogins = loginNicknameDict.Keys.Except(logins.Keys).Select(x => new TMFLogin
        {
            Id = x,
            Nickname = loginNicknameDict[x],
            NicknameDeformatted = TextFormatter.Deformat(loginNicknameDict[x])
        }).ToList();

        if (missingLogins.Count == 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            return logins;
        }

        if (!deadend && enableDetails)
        {
            foreach (var login in missingLogins)
            {
                RateLimitWhenCreating:

                if (rateLimited)
                {
                    try
                    {
                        var player = await ul.GetPlayerAsync(login.Id, cancellationToken);
                        login.RegistrationId = player.Id;
                    }
                    catch (Exception ex)
                    {
                        deadend = true;
                        logger.LogWarning(ex, "UnitedLadder lookup failed while creating TMF login {Login}, will not try again...", login.Id);
                        break;
                    }
                }
                else
                {
                    try
                    {
                        var player = await ws.GetPlayerAsync(login.Id, cancellationToken);
                        login.RegistrationId = player.Id;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                    {
                        rateLimited = true;
                        logger.LogWarning("Rate limited reached while creating TMF login {Login}, falling back to UnitedLadder API...", login.Id);
                        goto RateLimitWhenCreating;
                    }
                }
            }
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
        ArgumentNullException.ThrowIfNull(login);

        return await db.TMFLogins.FirstOrDefaultAsync(x => x.Id == login, cancellationToken);
    }

    public async Task<TMFLoginDto?> GetTMFDtoAsync(string login, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(login);

        return await db.TMFLogins
            .Include(x => x.Users)
            .Select(x => new TMFLoginDto
            {
                Id = x.Id,
                Nickname = x.Nickname,
                NicknameDeformatted = x.NicknameDeformatted,
                Users = x.Users.Select(u => new UserDto
                {
                    Guid = u.Guid
                }).ToImmutableList(),
            })
            .FirstOrDefaultAsync(x => x.Id == login, cancellationToken);
    }

    public async ValueTask<IEnumerable<TMFLogin>> GetMultipleTMFAsync(IEnumerable<string> logins, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logins);

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
        ArgumentNullException.ThrowIfNull(logins);

        if (!logins.Any())
        {
            return new Dictionary<string, string?>();
        }

        return await db.TMFLogins
            .Where(x => logins.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Nickname, cancellationToken);
    }
}
