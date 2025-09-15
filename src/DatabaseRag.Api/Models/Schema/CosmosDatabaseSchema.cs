namespace DatabaseRag.Api.Models.Schema;

/// <summary>
/// Schema information for a Cosmos database
/// </summary>
public class CosmosDatabaseSchema
{
    /// <summary>
    /// Name of the Cosmos database
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of containers in the database
    /// </summary>
    public List<CosmosContainerSchema> Containers { get; set; } = new();

    /// <summary>
    /// When the schema information was retrieved
    /// </summary>
    public DateTime RetrievedAtUtc { get; set; }
}