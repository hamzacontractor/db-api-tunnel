namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Constraint information for a SQL Server table
/// </summary>
public class ConstraintSchemaResponse
{
    /// <summary>
    /// Name of the constraint
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the constraint (PRIMARY KEY, UNIQUE, CHECK, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// List of columns involved in the constraint
    /// </summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Definition of the constraint (for CHECK constraints)
    /// </summary>
    public string? Definition { get; set; }

    /// <summary>
    /// Whether the constraint is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}