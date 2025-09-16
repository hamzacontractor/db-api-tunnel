using System.Text.Json.Serialization;

namespace Database.Api.Tunnel.Models.Schema;

/// <summary>
/// Schema information for a Cosmos database
/// </summary>
public class CosmosDatabaseSchema
{
    /// <summary>
    /// Name of the Cosmos database
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of containers in the database
    /// </summary>
    [JsonPropertyName("containers")]
    public List<CosmosContainerSchema> Containers { get; set; } = new();

    /// <summary>
    /// When the schema information was retrieved
    /// </summary>
    [JsonPropertyName("retrievedAtUtc")]
    public DateTime RetrievedAtUtc { get; set; }

    /// <summary>
    /// Summary statistics about the schema
    /// </summary>
    [JsonPropertyName("containerCount")]
    public int ContainerCount => Containers?.Count ?? 0;

    /// <summary>
    /// Total properties across all containers
    /// </summary>
    [JsonPropertyName("totalProperties")]
    public int TotalProperties => Containers?.Sum(c => c.Properties?.Count ?? 0) ?? 0;
}