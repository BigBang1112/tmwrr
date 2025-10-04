using Microsoft.AspNetCore.Http.HttpResults;
using TMWRR.Api;
using TMWRR.Extensions;
using TMWRR.Services;

namespace TMWRR.Endpoints;

public static class EnvironmentEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Environment");

        group.MapGet("/", GetEnvironments)
            .WithSummary("Environments")
            .WithDescription("Retrieve a list of all available environments.")
            .CacheOutput(CachePolicy.Environments);

        group.MapGet("/{id}", GetEnvironment)
            .WithSummary("Environment by ID")
            .WithDescription("Retrieve details of a specific environment by its ID.")
            .CacheOutput(CachePolicy.Environments);
    }

    private static async Task<Ok<IEnumerable<TMEnvironment>>> GetEnvironments(
        IEnvironmentService environmentService,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var dtos = await environmentService.GetAllDtosAsync(cancellationToken);

        response.ClientCache();

        return TypedResults.Ok(dtos);
    }

    private static async Task<Results<Ok<TMEnvironment>, ValidationProblem, NotFound>> GetEnvironment(
        string id,
        IEnvironmentService environmentService,
        HttpResponse response,
        CancellationToken cancellationToken)
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

        response.ClientCache();

        return TypedResults.Ok(dto);
    }
}
