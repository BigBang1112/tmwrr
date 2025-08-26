using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos.TMF;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class GamesTMFLoginsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id}", GetLogin);
    }

    private static async Task<Results<Ok<TMFLoginDto>, ValidationProblem, NotFound>> GetLogin(string id, ILoginService loginService, CancellationToken cancellationToken)
    {
        if (id.Length > 32)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(id)] = ["The login ID length must not exceed 32 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await loginService.GetTMFDtoAsync(id, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}