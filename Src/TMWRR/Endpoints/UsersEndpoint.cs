using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class UsersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{guid}", GetUser);
    }

    private static async Task<Results<Ok<UserDto>, NotFound>> GetUser(Guid guid, IUserService userService, CancellationToken cancellationToken)
    {
        var dto = await userService.GetDtoAsync(guid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}