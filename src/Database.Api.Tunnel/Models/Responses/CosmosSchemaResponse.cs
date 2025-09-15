using Database.Api.Tunnel.Models.Schema;

namespace Database.Api.Tunnel.Models.Responses;

/// <summary>
/// Response model for Cosmos DB schema information
/// </summary>
public class CosmosSchemaResponse
{
    /// <summary>
    /// Indicates whether the schema retrieval was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The Cosmos database schema information
    /// </summary>
    public CosmosDatabaseSchema? Schema { get; set; }

    /// <summary>
    /// Error message if the schema retrieval failed
    /// </summary>
    public string? Error { get; set; }
}