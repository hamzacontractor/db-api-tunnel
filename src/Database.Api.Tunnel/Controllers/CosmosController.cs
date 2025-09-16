using Database.Api.Tunnel.Models.Requests;
using Database.Api.Tunnel.Models.Responses;
using Database.Api.Tunnel.Services.Abstractions;

namespace Database.Api.Tunnel.Controllers;

/// <summary>
/// Cosmos DB endpoint mappings
/// </summary>
public static class CosmosController
{
    /// <summary>
    /// Maps Cosmos DB endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapCosmosEndpoints(this WebApplication app)
    {
        var cosmosGroup = app.MapGroup("/api/cosmos")
            .WithTags("Cosmos DB")
            .WithDescription("Cosmos DB database operations");

        // Cosmos DB schema endpoint
        cosmosGroup.MapPost("/schema", async (CosmosSchemaRequest request, HttpContext context, ICosmosService cosmosService) =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] Starting schema request for database: {request.DatabaseName}");

                // Get connection string from header
                if (!context.Request.Headers.TryGetValue("ConnectionString", out var connectionStringValues))
                {
                    System.Diagnostics.Debug.WriteLine("[SCHEMA ENDPOINT] ERROR: Missing ConnectionString header");
                    return Results.BadRequest(new { error = "ConnectionString header is required" });
                }

                var connectionString = connectionStringValues.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    System.Diagnostics.Debug.WriteLine("[SCHEMA ENDPOINT] ERROR: Empty ConnectionString header");
                    return Results.BadRequest(new { error = "ConnectionString header cannot be empty" });
                }

                if (string.IsNullOrWhiteSpace(request.DatabaseName))
                {
                    System.Diagnostics.Debug.WriteLine("[SCHEMA ENDPOINT] ERROR: Missing DatabaseName");
                    return Results.BadRequest(new { error = "DatabaseName is required" });
                }

                System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] Calling cosmos service for database: {request.DatabaseName}");
                var response = await cosmosService.GetSchemaAsync(request, connectionString, context.RequestAborted);

                System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] Service returned: Success={response.Success}");
                if (response.Success && response.Schema != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] Schema contains {response.Schema.Containers?.Count ?? 0} containers");
                    if (response.Schema.Containers != null)
                    {
                        foreach (var container in response.Schema.Containers.Take(3))
                        {
                            System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] Container: {container.Name} with {container.Properties?.Count ?? 0} properties");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] Error in response: {response.Error}");
                }

                if (response.Success)
                {
                    System.Diagnostics.Debug.WriteLine("[SCHEMA ENDPOINT] Returning OK result");
                    return Results.Ok(response);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SCHEMA ENDPOINT] Returning BadRequest result");
                    return Results.BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SCHEMA ENDPOINT] STACK TRACE: {ex.StackTrace}");

                var errorResponse = new CosmosSchemaResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Schema = null
                };

                return Results.BadRequest(errorResponse);
            }
        })
        .WithName("GetCosmosSchema")
        .WithSummary("Retrieves Cosmos DB database schema")
        .WithDescription("Gets schema information for Cosmos DB including containers and inferred document properties")
        .Produces<CosmosSchemaResponse>(200)
        .Produces<CosmosSchemaResponse>(400);

        // Cosmos DB connection test endpoint
        cosmosGroup.MapPost("/test", async (CosmosTestRequest request, HttpContext context, ICosmosService cosmosService) =>
        {
            try
            {
                // Get connection string from header
                if (!context.Request.Headers.TryGetValue("ConnectionString", out var connectionStringValues))
                {
                    return Results.BadRequest(new { error = "ConnectionString header is required" });
                }

                var connectionString = connectionStringValues.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return Results.BadRequest(new { error = "ConnectionString header cannot be empty" });
                }

                if (string.IsNullOrWhiteSpace(request.DatabaseName))
                {
                    return Results.BadRequest(new { error = "DatabaseName is required" });
                }

                var response = await cosmosService.TestConnectionAsync(request, connectionString, context.RequestAborted);
                var isHealthy = response.Status == "healthy";

                return isHealthy
                    ? Results.Ok(new { success = true, message = "Connection successful" })
                    : Results.BadRequest(new { success = false, error = response.Status });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { success = false, error = ex.Message });
            }
        })
        .WithName("TestCosmosConnection")
        .WithSummary("Tests Cosmos DB database connection")
        .WithDescription("Validates that the provided connection string can successfully connect to the Cosmos DB database")
        .Produces<object>(200)
        .Produces<object>(400);

        // Cosmos DB query endpoint
        cosmosGroup.MapPost("/query", async (CosmosQueryRequest request, HttpContext context, ICosmosService cosmosService) =>
        {
            try
            {
                // Get connection string from header
                if (!context.Request.Headers.TryGetValue("ConnectionString", out var connectionStringValues))
                {
                    return Results.BadRequest(new { error = "ConnectionString header is required" });
                }

                var connectionString = connectionStringValues.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return Results.BadRequest(new { error = "ConnectionString header cannot be empty" });
                }

                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                if (string.IsNullOrWhiteSpace(request.DatabaseName))
                {
                    return Results.BadRequest(new { error = "DatabaseName is required" });
                }

                if (string.IsNullOrWhiteSpace(request.ContainerName))
                {
                    return Results.BadRequest(new { error = "ContainerName is required" });
                }

                var response = await cosmosService.ExecuteQueryAsGenericAsync(request, connectionString, context.RequestAborted);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new GenericQueryResponse
                {
                    Description = $"Query execution failed: {ex.Message}",
                    Data = new QueryData(),
                    Conclusion = "An error occurred while executing the query",
                    Metadata = new Meta
                    {
                        Source = "Cosmos DB",
                        Properties = new Dictionary<string, object?>
                        {
                            ["error"] = ex.Message,
                            ["query"] = request.Query,
                            ["databaseName"] = request.DatabaseName,
                            ["containerName"] = request.ContainerName,
                            ["timestamp"] = DateTime.UtcNow
                        }
                    }
                };

                return Results.BadRequest(errorResponse);
            }
        })
        .WithName("ExecuteCosmosQuery")
        .WithSummary("Executes a Cosmos DB SQL query")
        .WithDescription("Executes a SQL query against the specified Cosmos DB container and returns standardized results with columns and rows")
        .Produces<GenericQueryResponse>(200)
        .Produces<GenericQueryResponse>(400);

        return app;
    }
}