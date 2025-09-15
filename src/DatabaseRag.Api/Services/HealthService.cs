using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Services.Abstractions;

namespace DatabaseRag.Api.Services;

/// <summary>
/// Service implementation for health check operations
/// </summary>
public class HealthService : IHealthService
{
    /// <summary>
    /// Performs a health check on the service
    /// </summary>
    /// <returns>Health response with current status</returns>
    public HealthResponse CheckHealth()
    {
        return new HealthResponse("healthy", DateTime.UtcNow, "DatabaseRag.Api");
    }
}