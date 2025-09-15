using Database.Api.Tunnel.Models.Responses;

namespace Database.Api.Tunnel.Services.Abstractions;

/// <summary>
/// Service interface for health check operations
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Performs a health check on the service
    /// </summary>
    /// <returns>Health response with current status</returns>
    HealthResponse CheckHealth();
}