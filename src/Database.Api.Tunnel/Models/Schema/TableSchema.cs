namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Schema information for a SQL Server table
/// </summary>
public class TableSchema
{
    /// <summary>
    /// Schema name (e.g., dbo)
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Table name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of columns in the table
    /// </summary>
    public IReadOnlyList<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();

    /// <summary>
    /// List of indexes on the table
    /// </summary>
    public IReadOnlyList<IndexSchemaResponse> Indexes { get; set; } = new List<IndexSchemaResponse>();

    /// <summary>
    /// List of constraints on the table
    /// </summary>
    public IReadOnlyList<ConstraintSchemaResponse> Constraints { get; set; } = new List<ConstraintSchemaResponse>();
}