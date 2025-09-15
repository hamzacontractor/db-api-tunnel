namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Schema information for a SQL Server table column
/// </summary>
public class ColumnSchema
{
    /// <summary>
    /// Column name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SQL data type
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column allows NULL values
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Whether the column is part of the primary key
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Maximum length for character data types
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Numeric precision for numeric data types
    /// </summary>
    public byte? NumericPrecision { get; set; }

    /// <summary>
    /// Numeric scale for numeric data types
    /// </summary>
    public int? NumericScale { get; set; }

    /// <summary>
    /// Default value constraint
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether the column is an identity column
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Whether the column is computed
    /// </summary>
    public bool IsComputed { get; set; }

    /// <summary>
    /// Ordinal position of the column in the table
    /// </summary>
    public int OrdinalPosition { get; set; }
}