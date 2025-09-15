using Database.Api.Tunnel.Models.Requests;
using Database.Api.Tunnel.Models.Responses;

namespace Database.Api.Tunnel.Services.Abstractions;

/// <summary>
/// Service interface for SQL Server operations
/// </summary>
public interface ISqlService
{
    /// <summary>
    /// Retrieves the schema information for a SQL Server database
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database schema response</returns>
    Task<DatabaseSchemaResponse> GetSchemaAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to a SQL Server database
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health response indicating connection status</returns>
    Task<HealthResponse> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a SQL query against a SQL Server database
    /// </summary>
    /// <param name="request">The SQL query request</param>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SQL query response with results</returns>
    Task<SqlQueryResponse> ExecuteQueryAsync(SqlQueryRequest request, string connectionString, CancellationToken cancellationToken = default);
}