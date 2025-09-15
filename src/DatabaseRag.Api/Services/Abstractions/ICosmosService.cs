using DatabaseRag.Api.Models.Requests;
using DatabaseRag.Api.Models.Responses;

namespace DatabaseRag.Api.Services.Abstractions;

/// <summary>
/// Service interface for Cosmos DB operations
/// </summary>
public interface ICosmosService
{
    /// <summary>
    /// Retrieves the schema information for a Cosmos database
    /// </summary>
    /// <param name="request">The Cosmos schema request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cosmos schema response</returns>
    Task<CosmosSchemaResponse> GetSchemaAsync(CosmosSchemaRequest request, string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to a Cosmos database
    /// </summary>
    /// <param name="request">The Cosmos test request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health response indicating connection status</returns>
    Task<HealthResponse> TestConnectionAsync(CosmosTestRequest request, string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a SQL query against a Cosmos database
    /// </summary>
    /// <param name="request">The Cosmos query request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cosmos query response with results</returns>
    Task<CosmosQueryResponse> ExecuteQueryAsync(CosmosQueryRequest request, string connectionString, CancellationToken cancellationToken = default);
}