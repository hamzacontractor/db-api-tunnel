using Database.Api.Tunnel.Models.Responses;
using Database.Api.Tunnel.Services.Abstractions;

namespace Database.Api.Tunnel.Services;

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