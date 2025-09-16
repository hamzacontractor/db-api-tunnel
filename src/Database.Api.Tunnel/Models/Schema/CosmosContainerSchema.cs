using System.Text.Json.Serialization;

namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Schema information for a Cosmos container
/// </summary>
public class CosmosContainerSchema
{
    /// <summary>
    /// Name of the container
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Partition key path for the container
    /// </summary>
    [JsonPropertyName("partitionKeyPath")]
    public string PartitionKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// List of properties found in documents within the container
    /// </summary>
    [JsonPropertyName("properties")]
    public List<CosmosPropertySchema> Properties { get; set; } = new();

    /// <summary>
    /// Number of properties in this container
    /// </summary>
    [JsonPropertyName("propertyCount")]
    public int PropertyCount => Properties?.Count ?? 0;

    /// <summary>
    /// Indicates if the container has any business-relevant properties (non-system)
    /// </summary>
    [JsonPropertyName("hasBusinessProperties")]
    public bool HasBusinessProperties => Properties?.Any(p => !IsSystemProperty(p.Name)) ?? false;

    /// <summary>
    /// Checks if a property name is a system property
    /// </summary>
    private static bool IsSystemProperty(string propertyName)
    {
        var systemProperties = new[] { "id", "_rid", "_self", "_etag", "_attachments", "_ts" };
        return systemProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }
}