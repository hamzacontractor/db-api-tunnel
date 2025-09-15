namespace DatabaseRag.Api.Models.Schema;

/// <summary>
/// Detailed information about SQL Server database schema
/// </summary>
public class DatabaseSchemaInfo
{
    /// <summary>
    /// List of tables in the database
    /// </summary>
    public List<TableSchema> Tables { get; set; } = new();

    /// <summary>
    /// List of foreign key relationships
    /// </summary>
    public List<ForeignKeySchemaResponse> ForeignKeys { get; set; } = new();

    /// <summary>
    /// Name of the database
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// SQL Server version information
    /// </summary>
    public string ServerVersion { get; set; } = string.Empty;

    /// <summary>
    /// When the schema information was retrieved
    /// </summary>
    public DateTime RetrievedAtUtc { get; set; }
}