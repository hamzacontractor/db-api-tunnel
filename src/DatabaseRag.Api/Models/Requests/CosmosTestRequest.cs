namespace DatabaseRag.Api.Models.Requests;

/// <summary>
/// Request model for testing Cosmos DB connection
/// </summary>
/// <param name="DatabaseName">The name of the Cosmos database to test</param>
public record CosmosTestRequest(string DatabaseName);