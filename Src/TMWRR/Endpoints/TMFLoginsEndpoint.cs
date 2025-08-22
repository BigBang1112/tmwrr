using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Immutable;
using TMWRR.Dtos;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class TMFLoginsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id}", GetLogin);
    }

    private static async Task<Results<Ok<TMFLoginDto>, NotFound>> GetLogin(string id, ILoginService loginService, CancellationToken cancellationToken)
    {
        var login = await loginService.GetTMFWithUsersAsync(id, cancellationToken);

        if (login is null)
        {
            return TypedResults.NotFound();
        }

        var dto = new TMFLoginDto
        {
            Id = login.Id,
            Nickname = login.Nickname,
            Users = login.Users?.Select(u => new UserDto
            {
                Guid = u.Guid,
                //LoginTMF = u.LoginTMF, // redundant, but once extracted to mapping extension methods, this should map empty
            }).ToImmutableArray() ?? []
        };

        return TypedResults.Ok(dto);
    }
}