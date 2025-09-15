using Database.Api.Tunnel.Services.Abstractions;

namespace Database.Api.Tunnel.Controllers;

/// <summary>
/// Health check endpoint mappings
/// </summary>
public static class HealthController
{
    /// <summary>
    /// Maps health check endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        // Health check endpoint
        app.MapGet("/health", (IHealthService healthService) =>
        {
            var response = healthService.CheckHealth();
            return Results.Ok(response);
        })
        .WithName("HealthCheck")
        .WithTags("Health")
        .WithSummary("Performs a health check on the service")
        .WithDescription("Returns the current health status of the DatabaseRag API service")
        .Produces<Models.Responses.HealthResponse>(200)
        .ProducesProblem(500);

        return app;
    }
}