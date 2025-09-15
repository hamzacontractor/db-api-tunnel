using DatabaseRag.Api.Models.Requests;
using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Services.Abstractions;

namespace DatabaseRag.Api.Controllers;

/// <summary>
/// SQL Server endpoint mappings
/// </summary>
public static class SqlController
{
    /// <summary>
    /// Maps SQL Server endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapSqlEndpoints(this WebApplication app)
    {
        var sqlGroup = app.MapGroup("/api/sql")
            .WithTags("SQL Server")
            .WithDescription("SQL Server database operations");

        // SQL Server schema endpoint
        sqlGroup.MapPost("/schema", async (HttpContext context, ISqlService sqlService) =>
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

                var response = await sqlService.GetSchemaAsync(connectionString, context.RequestAborted);
                return response.Success ? Results.Ok(response) : Results.BadRequest(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new DatabaseSchemaResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Schema = null
                };

                return Results.BadRequest(errorResponse);
            }
        })
        .WithName("GetSqlSchema")
        .WithSummary("Retrieves SQL Server database schema")
        .WithDescription("Gets comprehensive schema information including tables, columns, indexes, constraints, and foreign keys")
        .Produces<DatabaseSchemaResponse>(200)
        .Produces<DatabaseSchemaResponse>(400);

        // SQL Server connection test endpoint
        sqlGroup.MapPost("/test", async (HttpContext context, ISqlService sqlService) =>
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

                var response = await sqlService.TestConnectionAsync(connectionString, context.RequestAborted);
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
        .WithName("TestSqlConnection")
        .WithSummary("Tests SQL Server database connection")
        .WithDescription("Validates that the provided connection string can successfully connect to the SQL Server database")
        .Produces<object>(200)
        .Produces<object>(400);

        // SQL Server query endpoint
        sqlGroup.MapPost("/query", async (SqlQueryRequest request, HttpContext context, ISqlService sqlService) =>
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

                var response = await sqlService.ExecuteQueryAsync(request, connectionString, context.RequestAborted);
                return response.Success ? Results.Ok(response) : Results.BadRequest(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new SqlQueryResponse
                {
                    Success = false,
                    Error = ex.Message,
                    AnalyticalDetails = new Models.Schema.AnalyticalDetails
                    {
                        RowCount = 0,
                        ColumnCount = 0,
                        ExecutionTimeMs = 0,
                        Query = request.Query,
                        Timestamp = DateTime.UtcNow,
                        Columns = new List<Models.Schema.ColumnMetadata>()
                    }
                };

                return Results.BadRequest(errorResponse);
            }
        })
        .WithName("ExecuteSqlQuery")
        .WithSummary("Executes a SQL query")
        .WithDescription("Executes a SQL query against the specified SQL Server database and returns the results with analytical details")
        .Produces<SqlQueryResponse>(200)
        .Produces<SqlQueryResponse>(400);

        return app;
    }
}