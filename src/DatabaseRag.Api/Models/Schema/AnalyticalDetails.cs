namespace DatabaseRag.Api.Models.Schema;

/// <summary>
/// Analytical details for SQL query execution
/// </summary>
public class AnalyticalDetails
{
    /// <summary>
    /// Number of rows returned by the query
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Number of columns in the result set
    /// </summary>
    public int ColumnCount { get; set; }

    /// <summary>
    /// Query execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Metadata about the columns in the result set
    /// </summary>
    public List<ColumnMetadata> Columns { get; set; } = new();

    /// <summary>
    /// The executed SQL query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// When the query was executed
    /// </summary>
    public DateTime Timestamp { get; set; }
}