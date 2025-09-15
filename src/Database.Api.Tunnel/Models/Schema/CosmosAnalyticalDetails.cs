namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Analytical details for Cosmos DB query execution
/// </summary>
public class CosmosAnalyticalDetails
{
    /// <summary>
    /// Number of items returned by the query
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Query execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Request units consumed by the query
    /// </summary>
    public double RequestCharge { get; set; }

    /// <summary>
    /// Unique activity ID for the query execution
    /// </summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>
    /// The executed SQL query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The target database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// The target container name
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// When the query was executed
    /// </summary>
    public DateTime Timestamp { get; set; }

    // Enhanced AI-consumable metadata
    /// <summary>
    /// Type of query being executed
    /// </summary>
    public string QueryType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the query targets a specific container
    /// </summary>
    public bool IsContainerSpecific { get; set; }

    /// <summary>
    /// Number of business-relevant columns
    /// </summary>
    public int BusinessColumnCount { get; set; }

    /// <summary>
    /// Total number of columns in the result
    /// </summary>
    public int TotalColumns { get; set; }

    /// <summary>
    /// Whether the result contains business-relevant data
    /// </summary>
    public bool HasBusinessData { get; set; }

    /// <summary>
    /// Complexity level of the schema
    /// </summary>
    public string SchemaComplexity { get; set; } = string.Empty;

    /// <summary>
    /// Quality assessment of the JSON structure in results
    /// </summary>
    public string JsonQuality { get; set; } = string.Empty;
}