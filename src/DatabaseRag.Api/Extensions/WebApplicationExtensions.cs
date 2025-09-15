using DatabaseRag.Api.Controllers;

namespace DatabaseRag.Api.Extensions;

/// <summary>
/// Extension methods for endpoint registration
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps all API endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        // Map health endpoints
        app.MapHealthEndpoints();

        // Map SQL endpoints
        app.MapSqlEndpoints();

        // Map Cosmos endpoints
        app.MapCosmosEndpoints();

        return app;
    }
}