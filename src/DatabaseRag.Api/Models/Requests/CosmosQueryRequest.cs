namespace DatabaseRag.Api.Models.Requests;

/// <summary>
/// Request model for executing Cosmos DB queries
/// </summary>
/// <param name="Query">The SQL query to execute against Cosmos DB</param>
/// <param name="DatabaseName">The name of the Cosmos database</param>
/// <param name="ContainerName">The name of the Cosmos container</param>
public record CosmosQueryRequest(string Query, string DatabaseName, string ContainerName);