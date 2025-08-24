using Microsoft.EntityFrameworkCore;
using TMWRR.Data;
using TMWRR.Dtos;

namespace TMWRR.Services;

public interface IUserService
{
    Task<UserDto?> GetDtoAsync(Guid guid, CancellationToken cancellationToken);
}

public sealed class UserService : IUserService
{
    private readonly AppDbContext db;

    public UserService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<UserDto?> GetDtoAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await db.Users
            .Select(x => new UserDto
            {
                Guid = x.Guid,
                LoginTMF = x.LoginTMF == null ? null : new TMFLoginDto
                {
                    Id = x.LoginTMF.Id,
                    Nickname = x.LoginTMF.Nickname,
                    NicknameDeformatted = x.LoginTMF.NicknameDeformatted,
                },
            })
            .FirstOrDefaultAsync(x => x.Guid == guid, cancellationToken);
    }
}
