using System.Text.Json.Serialization;

namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Property schema information for Cosmos documents
/// </summary>
public class CosmosPropertySchema
{
    /// <summary>
    /// Name of the property
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON type of the property
    /// </summary>
    [JsonPropertyName("jsonType")]
    public string JsonType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the property can be null
    /// </summary>
    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; set; }

    /// <summary>
    /// Indicates if this is a system property
    /// </summary>
    [JsonPropertyName("isSystemProperty")]
    public bool IsSystemProperty => IsSystemPropertyName(Name);

    /// <summary>
    /// Additional type information for complex types
    /// </summary>
    [JsonPropertyName("typeDetails")]
    public string? TypeDetails { get; set; }

    /// <summary>
    /// Checks if a property name is a system property
    /// </summary>
    private static bool IsSystemPropertyName(string propertyName)
    {
        var systemProperties = new[] { "id", "_rid", "_self", "_etag", "_attachments", "_ts" };
        return systemProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }
}