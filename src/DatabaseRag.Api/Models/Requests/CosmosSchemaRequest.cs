namespace DatabaseRag.Api.Models.Requests;

/// <summary>
/// Request model for retrieving Cosmos DB schema information
/// </summary>
/// <param name="DatabaseName">The name of the Cosmos database</param>
public record CosmosSchemaRequest(string DatabaseName);