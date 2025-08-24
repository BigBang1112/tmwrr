using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Dtos;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class EnvironmentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetEnvironments);
        group.MapGet("/{id}", GetEnvironment);
    }

    private static async Task<Ok<IEnumerable<TMEnvironmentDto>>> GetEnvironments(IEnvironmentService environmentService, CancellationToken cancellationToken)
    {
        var dtos = await environmentService.GetAllDtosAsync(cancellationToken);

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMEnvironmentDto>, ValidationProblem, NotFound>> GetEnvironment(string id, IEnvironmentService environmentService, CancellationToken cancellationToken)
    {
        if (id.Length > 16)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(id)] = ["The environment ID length must not exceed 16 characters."]
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dto = await environmentService.GetDtoAsync(id, cancellationToken);

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }
}
