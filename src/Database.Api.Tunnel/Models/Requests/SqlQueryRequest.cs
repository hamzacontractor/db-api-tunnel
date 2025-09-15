namespace Database.Api.Tunnel.Models.Requests;

/// <summary>
/// Request model for executing SQL queries
/// </summary>
/// <param name="Query">The SQL query to execute</param>
public record SqlQueryRequest(string Query);