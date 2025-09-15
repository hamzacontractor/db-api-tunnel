using Database.Api.Tunnel.Models.Schema;

namespace Database.Api.Tunnel.Models.Responses;

/// <summary>
/// Response model for SQL Server database schema information
/// </summary>
public class DatabaseSchemaResponse
{
    /// <summary>
    /// Indicates whether the schema retrieval was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The database schema information
    /// </summary>
    public DatabaseSchemaInfo? Schema { get; set; }

    /// <summary>
    /// Error message if the schema retrieval failed
    /// </summary>
    public string? Error { get; set; }
}