namespace Database.Api.Tunnel.Models.Responses;

/// <summary>
/// Standardized column definition for query results
/// </summary>
public class Column
{
    /// <summary>
    /// The name of the column
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The data type of the column (string, number, boolean, object, array, null)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column can contain null values
    /// </summary>
    public bool IsNullable { get; set; }
}

/// <summary>
/// Structured query data with standardized columns and rows
/// </summary>
public class QueryData
{
    /// <summary>
    /// Array of column definitions describing the structure
    /// </summary>
    public Column[] Columns { get; set; } = Array.Empty<Column>();

    /// <summary>
    /// Array of row data as dictionaries with consistent keys matching column names
    /// </summary>
    public Dictionary<string, object?>[] Rows { get; set; } = Array.Empty<Dictionary<string, object?>>();
}

/// <summary>
/// Optional metadata for additional context about the query source
/// </summary>
public class Meta
{
    /// <summary>
    /// Source system that generated the data (e.g., "Cosmos DB", "SQL Server")
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Additional context-specific metadata
    /// </summary>
    public Dictionary<string, object?>? Properties { get; set; }
}

/// <summary>
/// Standardized generic response format for all query endpoints
/// This is the canonical API output format that all query endpoints (Cosmos/SQL) should return
/// </summary>
public class GenericQueryResponse
{
    /// <summary>
    /// Human-readable description of the query results and any important context
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The structured query data with standardized column/row format
    /// </summary>
    public QueryData Data { get; set; } = new();

    /// <summary>
    /// Summary or interpretation of the results, useful for AI consumption
    /// </summary>
    public string Conclusion { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata about the source and execution context
    /// </summary>
    public Meta? Metadata { get; set; }
}