using System.Text.Json.Serialization;
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
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The Cosmos database schema information
    /// </summary>
    [JsonPropertyName("schema")]
    public CosmosDatabaseSchema? Schema { get; set; }

    /// <summary>
    /// Error message if the schema retrieval failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Summary of the schema for quick analysis
    /// </summary>
    [JsonPropertyName("summary")]
    public SchemaSummary? Summary => Schema != null ? new SchemaSummary
    {
        DatabaseName = Schema.Name,
        ContainerCount = Schema.ContainerCount,
        TotalProperties = Schema.TotalProperties,
        RetrievedAt = Schema.RetrievedAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC")
    } : null;
}

/// <summary>
/// Summary information about the schema
/// </summary>
public class SchemaSummary
{
    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("containerCount")]
    public int ContainerCount { get; set; }

    [JsonPropertyName("totalProperties")]
    public int TotalProperties { get; set; }

    [JsonPropertyName("retrievedAt")]
    public string RetrievedAt { get; set; } = string.Empty;
}