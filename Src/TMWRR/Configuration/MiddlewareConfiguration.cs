using HealthChecks.UI.Client;
using Scalar.AspNetCore;

namespace TMWRR.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        app.UseRateLimiter();

        app.UseOutputCache();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Theme = ScalarTheme.DeepSpace;
        });
    }
}
