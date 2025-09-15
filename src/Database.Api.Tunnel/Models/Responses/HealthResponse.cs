namespace Database.Api.Tunnel.Models.Responses;

/// <summary>
/// Health check response model
/// </summary>
/// <param name="Status">The health status</param>
/// <param name="Timestamp">The timestamp when the health check was performed</param>
/// <param name="Service">The name of the service</param>
public record HealthResponse(string Status, DateTime Timestamp, string Service);