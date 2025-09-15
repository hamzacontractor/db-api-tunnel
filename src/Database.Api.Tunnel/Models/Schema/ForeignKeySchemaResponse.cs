namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Foreign key relationship information
/// </summary>
public class ForeignKeySchemaResponse
{
    /// <summary>
    /// Name of the foreign key constraint
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Schema of the referencing table
    /// </summary>
    public string TableSchema { get; set; } = string.Empty;

    /// <summary>
    /// Name of the referencing table
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the referencing column
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Schema of the referenced table
    /// </summary>
    public string ReferencedTableSchema { get; set; } = string.Empty;

    /// <summary>
    /// Name of the referenced table
    /// </summary>
    public string ReferencedTableName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the referenced column
    /// </summary>
    public string ReferencedColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Delete rule for the foreign key
    /// </summary>
    public string DeleteRule { get; set; } = string.Empty;

    /// <summary>
    /// Update rule for the foreign key
    /// </summary>
    public string UpdateRule { get; set; } = string.Empty;
}