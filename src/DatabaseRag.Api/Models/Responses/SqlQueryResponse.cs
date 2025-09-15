using DatabaseRag.Api.Models.Schema;

namespace DatabaseRag.Api.Models.Responses;

/// <summary>
/// Response model for SQL query execution
/// </summary>
public class SqlQueryResponse
{
    /// <summary>
    /// Indicates whether the query execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The query result data
    /// </summary>
    public List<Dictionary<string, object>>? Data { get; set; }

    /// <summary>
    /// Error message if the query failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Analytical details about the query execution
    /// </summary>
    public AnalyticalDetails? AnalyticalDetails { get; set; }
}