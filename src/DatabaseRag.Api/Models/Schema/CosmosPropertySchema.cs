namespace DatabaseRag.Api.Models.Schema;

/// <summary>
/// Property schema information for Cosmos documents
/// </summary>
public class CosmosPropertySchema
{
    /// <summary>
    /// Name of the property
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON type of the property
    /// </summary>
    public string JsonType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the property can be null
    /// </summary>
    public bool IsNullable { get; set; }
}