namespace DatabaseRag.Api.Models.Schema;

/// <summary>
/// Schema information for a Cosmos container
/// </summary>
public class CosmosContainerSchema
{
    /// <summary>
    /// Name of the container
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Partition key path for the container
    /// </summary>
    public string PartitionKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// List of properties found in documents within the container
    /// </summary>
    public List<CosmosPropertySchema> Properties { get; set; } = new();
}