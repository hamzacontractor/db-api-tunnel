namespace DatabaseRag.Api.Models.Schema;

/// <summary>
/// Metadata about a column in a SQL query result set
/// </summary>
public class ColumnMetadata
{
    /// <summary>
    /// Name of the column
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the column
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the column data
    /// </summary>
    public int ColumnSize { get; set; }

    /// <summary>
    /// Whether the column allows NULL values
    /// </summary>
    public bool AllowDBNull { get; set; }
}