namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Index information for a SQL Server table
/// </summary>
public class IndexSchemaResponse
{
    /// <summary>
    /// Name of the index
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of columns included in the index
    /// </summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Whether the index enforces uniqueness
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Whether this is the primary key index
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Whether the index is clustered
    /// </summary>
    public bool IsClustered { get; set; }

    /// <summary>
    /// Type of the index
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Filter definition for filtered indexes
    /// </summary>
    public string? FilterDefinition { get; set; }
}