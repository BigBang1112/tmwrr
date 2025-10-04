using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Api;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class UserEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Map");

        group.MapGet("/{guid}", GetUser)
            .WithSummary("User by GUID")
            .WithDescription("Retrieve details of a specific user by their GUID.");
    }

    private static async Task<Results<Ok<User>, NotFound>> GetUser(Guid guid, IUserService userService, CancellationToken cancellationToken)
    {
        var dto = await userService.GetDtoAsync(guid, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}