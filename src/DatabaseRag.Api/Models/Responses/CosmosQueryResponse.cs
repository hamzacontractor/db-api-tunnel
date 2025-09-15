using System.Text.Json;
using System.Text.Json.Serialization;
using DatabaseRag.Api.Models.Schema;

namespace DatabaseRag.Api.Models.Responses;

/// <summary>
/// Response model for Cosmos DB query execution with enhanced JSON formatting
/// </summary>
public class CosmosQueryResponse
{
    /// <summary>
    /// Indicates whether the query execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The query result data as proper JSON objects that can handle nested structures
    /// Each value can be either a string, number, boolean, null, or another nested object/array
    /// </summary>
    [JsonPropertyName("data")]
    public List<Dictionary<string, JsonElement>>? Data { get; set; }

    /// <summary>
    /// Error message if the query failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Analytical details about the Cosmos DB query execution
    /// </summary>
    [JsonPropertyName("analyticalDetails")]
    public CosmosAnalyticalDetails? AnalyticalDetails { get; set; }

    /// <summary>
    /// Total number of records returned
    /// </summary>
    [JsonPropertyName("totalRecords")]
    public int TotalRecords => Data?.Count ?? 0;

    /// <summary>
    /// Schema information inferred from the result set
    /// </summary>
    [JsonPropertyName("inferredSchema")]
    public List<CosmosPropertySchema>? InferredSchema { get; set; }
}