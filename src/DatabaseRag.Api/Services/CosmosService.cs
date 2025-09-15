using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DatabaseRag.Api.Models.Requests;
using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Models.Schema;
using DatabaseRag.Api.Services.Abstractions;
using DatabaseRag.Api.Utilities;
using Microsoft.Azure.Cosmos;

namespace DatabaseRag.Api.Services;

/// <summary>
/// Service implementation for Cosmos DB operations
/// </summary>
public class CosmosService : ICosmosService
{
    /// <summary>
    /// Retrieves the schema information for a Cosmos database
    /// </summary>
    /// <param name="request">The Cosmos schema request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cosmos schema response</returns>
    public async Task<CosmosSchemaResponse> GetSchemaAsync(CosmosSchemaRequest request, string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.DatabaseName))
            {
                throw new ArgumentException("DatabaseName is required");
            }

            var cosmosClient = new CosmosClient(connectionString);
            var database = cosmosClient.GetDatabase(request.DatabaseName);

            var containers = new List<CosmosContainerSchema>();

            // Get all containers
            var containerIterator = database.GetContainerQueryIterator<ContainerProperties>();
            while (containerIterator.HasMoreResults)
            {
                var response = await containerIterator.ReadNextAsync(cancellationToken);
                foreach (var containerProps in response)
                {
                    var container = database.GetContainer(containerProps.Id);

                    // Get the last document to infer schema (most recent document structure)
                    var lastDocQuery = new QueryDefinition("SELECT * FROM c ORDER BY c._ts DESC OFFSET 0 LIMIT 1");
                    var lastDocIterator = container.GetItemQueryIterator<dynamic>(lastDocQuery);

                    var properties = new List<CosmosPropertySchema>();
                    if (lastDocIterator.HasMoreResults)
                    {
                        var lastDocResponse = await lastDocIterator.ReadNextAsync(cancellationToken);
                        if (lastDocResponse.Any())
                        {
                            var lastDoc = lastDocResponse.First();
                            var docDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(lastDoc));

                            foreach (var prop in docDict!)
                            {
                                properties.Add(new CosmosPropertySchema
                                {
                                    Name = prop.Key,
                                    JsonType = GetDetailedJsonType(prop.Value),
                                    IsNullable = prop.Value.ValueKind == JsonValueKind.Null
                                });
                            }
                        }
                    }

                    containers.Add(new CosmosContainerSchema
                    {
                        Name = containerProps.Id,
                        PartitionKeyPath = containerProps.PartitionKeyPath ?? "/id",
                        Properties = properties
                    });
                }
            }

            var schema = new CosmosSchemaResponse
            {
                Success = true,
                Schema = new CosmosDatabaseSchema
                {
                    Name = request.DatabaseName,
                    Containers = containers,
                    RetrievedAtUtc = DateTime.UtcNow
                }
            };

            return schema;
        }
        catch (Exception ex)
        {
            return new CosmosSchemaResponse
            {
                Success = false,
                Error = ex.Message,
                Schema = null
            };
        }
    }

    /// <summary>
    /// Tests the connection to a Cosmos database
    /// </summary>
    /// <param name="request">The Cosmos test request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health response indicating connection status</returns>
    public async Task<HealthResponse> TestConnectionAsync(CosmosTestRequest request, string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.DatabaseName))
            {
                throw new ArgumentException("DatabaseName is required");
            }

            var cosmosClient = new CosmosClient(connectionString);
            var database = cosmosClient.GetDatabase(request.DatabaseName);
            var response = await database.ReadAsync(cancellationToken: cancellationToken);

            return new HealthResponse("healthy", DateTime.UtcNow, "Cosmos DB Connection");
        }
        catch (Exception ex)
        {
            return new HealthResponse($"unhealthy: {ex.Message}", DateTime.UtcNow, "Cosmos DB Connection");
        }
    }

    /// <summary>
    /// Executes a SQL query against a Cosmos database
    /// </summary>
    /// <param name="request">The Cosmos query request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cosmos query response with results</returns>
    public async Task<CosmosQueryResponse> ExecuteQueryAsync(CosmosQueryRequest request, string connectionString, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException("Query cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(request.DatabaseName))
            {
                throw new ArgumentException("DatabaseName is required");
            }

            if (string.IsNullOrWhiteSpace(request.ContainerName))
            {
                throw new ArgumentException("ContainerName is required");
            }

            Console.WriteLine($"[DEBUG] Cosmos Query: {request.Query}");
            Console.WriteLine($"[DEBUG] Database: {request.DatabaseName}, Container: {request.ContainerName}");

            // Validate the query for common Cosmos DB issues before execution
            try
            {
                // Apply query validation and cleaning
                var validatedQuery = ValidateAndCleanCosmosQuery(request.Query);
                if (validatedQuery != request.Query)
                {
                    Console.WriteLine($"[DEBUG] Query was cleaned/validated. Original: {request.Query}");
                    Console.WriteLine($"[DEBUG] Validated query: {validatedQuery}");
                    request = request with { Query = validatedQuery };
                }
            }
            catch (Exception validateEx)
            {
                Console.WriteLine($"[DEBUG] Query validation failed: {validateEx.Message}");
                // Continue with original query if validation fails
            }

            var cosmosClient = new CosmosClient(connectionString);
            var database = cosmosClient.GetDatabase(request.DatabaseName);
            var container = database.GetContainer(request.ContainerName);

            // Test a simple query first to ensure connection works
            try
            {
                var testQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                var testIterator = container.GetItemQueryIterator<int>(testQuery);
                var testResponse = await testIterator.ReadNextAsync(cancellationToken);
                var itemCount = testResponse.Resource.FirstOrDefault();
                Console.WriteLine($"[DEBUG] Container has {itemCount} total items");
            }
            catch (Exception testEx)
            {
                Console.WriteLine($"[DEBUG] Test query failed: {testEx.Message}");
                throw new InvalidOperationException($"Container access failed: {testEx.Message}", testEx);
            }

            var queryDefinition = new QueryDefinition(request.Query);
            var resultSetIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

            var results = new List<Dictionary<string, JsonElement>>();
            double requestCharge = 0;
            string activityId = string.Empty;
            int pageCount = 0;

            Console.WriteLine($"[DEBUG] Starting query execution: {request.Query}");

            while (resultSetIterator.HasMoreResults)
            {
                pageCount++;
                Console.WriteLine($"[DEBUG] Processing page {pageCount}");

                var response = await resultSetIterator.ReadNextAsync(cancellationToken);
                Console.WriteLine($"[DEBUG] Page {pageCount} returned {response.Count} items");

                if (response.Count > 0)
                {
                    var firstItemJson = JsonSerializer.Serialize(response.First());
                    Console.WriteLine($"[DEBUG] Page {pageCount} raw first item: {firstItemJson}");
                }

                requestCharge += response.RequestCharge;
                activityId = response.ActivityId;

                foreach (var item in response)
                {
                    try
                    {
                        var row = CosmosItemConverter.ConvertCosmosItemToDictionary(item);
                        results.Add(row);

                        // Log first item as sample with detailed info
                        if (results.Count == 1)
                        {
                            Console.WriteLine($"[DEBUG] Sample item keys: {string.Join(", ", row.Keys)}");
                            if (row.Count > 0)
                            {
                                foreach (var kvp in row)
                                {
                                    Console.WriteLine($"[DEBUG] Sample item first key-value: {kvp.Key} = {kvp.Value}");
                                    break; // Only show the first one
                                }
                            }
                        }
                    }
                    catch (Exception conversionEx)
                    {
                        Console.WriteLine($"[ERROR] Failed to convert item: {conversionEx.Message}");
                        Console.WriteLine($"[ERROR] Raw item: {JsonSerializer.Serialize(item)}");
                        throw; // Re-throw to surface the conversion issue
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Total results: {results.Count}");

            stopwatch.Stop();

            // Infer schema from the result set
            var inferredSchema = InferSchemaFromResults(results);

            var cosmosResponse = new CosmosQueryResponse
            {
                Success = true,
                Data = results,
                InferredSchema = inferredSchema,
                AnalyticalDetails = new CosmosAnalyticalDetails
                {
                    ItemCount = results.Count,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    RequestCharge = requestCharge,
                    ActivityId = activityId,
                    Query = request.Query,
                    DatabaseName = request.DatabaseName,
                    ContainerName = request.ContainerName,
                    Timestamp = DateTime.UtcNow,

                    // Enhanced metadata for better AI consumption
                    QueryType = "Cosmos DB SQL",
                    IsContainerSpecific = true,
                    BusinessColumnCount = CosmosResultAnalyzer.CountBusinessColumns(results),
                    TotalColumns = results.FirstOrDefault()?.Keys.Count ?? 0,
                    HasBusinessData = CosmosResultAnalyzer.HasBusinessRelevantData(results),
                    SchemaComplexity = CosmosResultAnalyzer.CalculateSchemaComplexity(results),
                    JsonQuality = CosmosResultAnalyzer.AnalyzeJsonQuality(results)
                }
            };

            return cosmosResponse;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new CosmosQueryResponse
            {
                Success = false,
                Error = ex.Message,
                AnalyticalDetails = new CosmosAnalyticalDetails
                {
                    ItemCount = 0,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    RequestCharge = 0,
                    Query = request.Query,
                    DatabaseName = request.DatabaseName,
                    ContainerName = request.ContainerName,
                    Timestamp = DateTime.UtcNow
                }
            };
        }
    }

    /// <summary>
    /// Executes a SQL query against a Cosmos database and returns standardized format
    /// </summary>
    /// <param name="request">The Cosmos query request</param>
    /// <param name="connectionString">The Cosmos DB connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generic query response with standardized format</returns>
    public async Task<GenericQueryResponse> ExecuteQueryAsGenericAsync(CosmosQueryRequest request, string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            // Execute the query using existing logic
            var cosmosResponse = await ExecuteQueryAsync(request, connectionString, cancellationToken);

            if (!cosmosResponse.Success)
            {
                return new GenericQueryResponse
                {
                    Description = $"Query execution failed: {cosmosResponse.Error}",
                    Data = new QueryData(),
                    Conclusion = "Query failed to execute successfully",
                    Metadata = new Meta
                    {
                        Source = "Cosmos DB",
                        Properties = new Dictionary<string, object?>
                        {
                            ["error"] = cosmosResponse.Error,
                            ["databaseName"] = request.DatabaseName,
                            ["containerName"] = request.ContainerName,
                            ["query"] = request.Query
                        }
                    }
                };
            }

            return MapCosmosResponseToGeneric(cosmosResponse, request);
        }
        catch (Exception ex)
        {
            return new GenericQueryResponse
            {
                Description = $"Query execution encountered an error: {ex.Message}",
                Data = new QueryData(),
                Conclusion = "Query execution failed due to an unexpected error",
                Metadata = new Meta
                {
                    Source = "Cosmos DB",
                    Properties = new Dictionary<string, object?>
                    {
                        ["error"] = ex.Message,
                        ["databaseName"] = request.DatabaseName,
                        ["containerName"] = request.ContainerName,
                        ["query"] = request.Query
                    }
                }
            };
        }
    }

    /// <summary>
    /// Maps CosmosQueryResponse to GenericQueryResponse format
    /// </summary>
    /// <param name="cosmosResponse">The original Cosmos response</param>
    /// <param name="request">The original request for context</param>
    /// <returns>Standardized generic response</returns>
    private static GenericQueryResponse MapCosmosResponseToGeneric(CosmosQueryResponse cosmosResponse, CosmosQueryRequest request)
    {
        var data = cosmosResponse.Data ?? new List<Dictionary<string, JsonElement>>();
        var details = cosmosResponse.AnalyticalDetails;

        // Extract columns deterministically
        var columns = CosmosResultAnalyzer.ExtractColumns(data);

        // Convert rows to standard format
        var rows = CosmosResultAnalyzer.ConvertToStandardRows(data);

        // Generate description
        var description = GenerateQueryDescription(data.Count, columns.Length, details);

        // Generate conclusion
        var conclusion = GenerateQueryConclusion(data, details);

        return new GenericQueryResponse
        {
            Description = description,
            Data = new QueryData
            {
                Columns = columns,
                Rows = rows
            },
            Conclusion = conclusion,
            Metadata = new Meta
            {
                Source = "Cosmos DB",
                Properties = new Dictionary<string, object?>
                {
                    ["databaseName"] = request.DatabaseName,
                    ["containerName"] = request.ContainerName,
                    ["executionTimeMs"] = details?.ExecutionTimeMs ?? 0,
                    ["requestCharge"] = details?.RequestCharge ?? 0,
                    ["activityId"] = details?.ActivityId,
                    ["totalRecords"] = data.Count,
                    ["businessColumnCount"] = details?.BusinessColumnCount ?? 0,
                    ["schemaComplexity"] = details?.SchemaComplexity,
                    ["jsonQuality"] = details?.JsonQuality,
                    ["queryType"] = details?.QueryType ?? "Cosmos DB SQL"
                }
            }
        };
    }

    /// <summary>
    /// Generates a human-readable description of the query results
    /// </summary>
    /// <param name="rowCount">Number of rows returned</param>
    /// <param name="columnCount">Number of columns in results</param>
    /// <param name="details">Analytical details from execution</param>
    /// <returns>Description string</returns>
    private static string GenerateQueryDescription(int rowCount, int columnCount, CosmosAnalyticalDetails? details)
    {
        if (rowCount == 0)
            return "Query executed successfully but returned no results.";

        var description = $"Query returned {rowCount} record{(rowCount == 1 ? "" : "s")} with {columnCount} column{(columnCount == 1 ? "" : "s")}.";

        if (details != null)
        {
            description += $" Execution completed in {details.ExecutionTimeMs}ms consuming {details.RequestCharge:F2} RU.";

            if (details.HasBusinessData)
            {
                description += $" Results contain {details.BusinessColumnCount} business-relevant column{(details.BusinessColumnCount == 1 ? "" : "s")}.";
            }
        }

        return description;
    }

    /// <summary>
    /// Generates a summary conclusion about the query results
    /// </summary>
    /// <param name="data">Query result data</param>
    /// <param name="details">Analytical details from execution</param>
    /// <returns>Conclusion string</returns>
    private static string GenerateQueryConclusion(List<Dictionary<string, JsonElement>> data, CosmosAnalyticalDetails? details)
    {
        if (data.Count == 0)
            return "No data was found matching the query criteria.";

        var conclusion = $"Successfully retrieved {data.Count} record{(data.Count == 1 ? "" : "s")} from Cosmos DB.";

        if (details != null)
        {
            if (!details.HasBusinessData)
            {
                conclusion += " Results contain primarily system metadata columns.";
            }
            else
            {
                switch (details.SchemaComplexity?.ToLower())
                {
                    case "simple":
                        conclusion += " Data has a simple structure with basic data types.";
                        break;
                    case "moderate":
                        conclusion += " Data has moderate complexity with some nested structures.";
                        break;
                    case "complex":
                    case "very complex":
                        conclusion += " Data contains complex nested objects and arrays.";
                        break;
                }
            }

            if (details.JsonQuality?.Contains("Poor") == true)
            {
                conclusion += " Note: Results contain a high proportion of null values.";
            }
        }

        return conclusion;
    }

    /// <summary>
    /// Gets detailed JSON type information from a JsonElement including array element types
    /// </summary>
    /// <param name="element">The JsonElement to analyze</param>
    /// <returns>Detailed type description</returns>
    private static string GetDetailedJsonType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "null",
            JsonValueKind.String => "string",
            JsonValueKind.Number => element.TryGetInt32(out _) ? "integer" : "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Object => "object",
            JsonValueKind.Array => GetArrayType(element),
            _ => "unknown"
        };
    }

    /// <summary>
    /// Determines the array type by examining array elements
    /// </summary>
    /// <param name="arrayElement">The JsonElement representing an array</param>
    /// <returns>Array type description</returns>
    private static string GetArrayType(JsonElement arrayElement)
    {
        if (arrayElement.GetArrayLength() == 0)
        {
            return "array"; // Empty array - generic type
        }

        var firstElement = arrayElement.EnumerateArray().First();
        var elementType = GetDetailedJsonType(firstElement);
        return $"array<{elementType}>";
    }

    /// <summary>
    /// Infers schema from query results by analyzing the structure of returned data
    /// </summary>
    /// <param name="results">Query results to analyze with JsonElement values</param>
    /// <returns>List of property schemas</returns>
    private static List<CosmosPropertySchema> InferSchemaFromResults(List<Dictionary<string, JsonElement>> results)
    {
        var schema = new List<CosmosPropertySchema>();

        if (!results.Any()) return schema;

        // Use the first result to infer schema structure
        var sampleResult = results.First();

        foreach (var kvp in sampleResult)
        {
            var propertyName = kvp.Key;
            var propertyValue = kvp.Value;

            schema.Add(new CosmosPropertySchema
            {
                Name = propertyName,
                JsonType = InferTypeFromJsonElement(propertyValue),
                IsNullable = propertyValue.ValueKind == JsonValueKind.Null
            });
        }

        return schema;
    }

    /// <summary>
    /// Infers JSON type from a JsonElement
    /// </summary>
    /// <param name="element">The JsonElement to analyze</param>
    /// <returns>JSON type description</returns>
    private static string InferTypeFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "null",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Array => $"array<{(element.GetArrayLength() > 0 ? InferTypeFromJsonElement(element.EnumerateArray().First()) : "unknown")}>",
            JsonValueKind.Object => "object",
            JsonValueKind.Undefined => "undefined",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Validates and cleans a Cosmos DB SQL query to fix common syntax issues.
    /// </summary>
    /// <param name="query">The raw Cosmos DB SQL query.</param>
    /// <returns>The cleaned and validated query.</returns>
    private static string ValidateAndCleanCosmosQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        var cleanedQuery = query.Trim();

        // Remove markdown code blocks if present
        cleanedQuery = Regex.Replace(cleanedQuery, @"```(?:sql|cosmos)?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        cleanedQuery = cleanedQuery.Replace("```", "").Trim();

        // Replace SQL Server specific functions with Cosmos DB equivalents
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bISNULL\(", "COALESCE(", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bLEN\(", "LENGTH(", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEADD\(", "DateTimeAdd(", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEDIFF\(", "DateTimeDiff(", RegexOptions.IgnoreCase);

        // Fix DATEPART function - replace with appropriate Cosmos DB alternatives
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*YEAR\s*,\s*([^)]+)\)", "DateTimePart(\"year\", $1)", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*MONTH\s*,\s*([^)]+)\)", "DateTimePart(\"month\", $1)", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*DAY\s*,\s*([^)]+)\)", "DateTimePart(\"day\", $1)", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*HOUR\s*,\s*([^)]+)\)", "DateTimePart(\"hour\", $1)", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*MINUTE\s*,\s*([^)]+)\)", "DateTimePart(\"minute\", $1)", RegexOptions.IgnoreCase);
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*SECOND\s*,\s*([^)]+)\)", "DateTimePart(\"second\", $1)", RegexOptions.IgnoreCase);
        // Generic DATEPART fallback
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bDATEPART\s*\(\s*([^,]+)\s*,\s*([^)]+)\)", "DateTimePart($1, $2)", RegexOptions.IgnoreCase);

        // Fix STRING function - replace with ToString()
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bSTRING\s*\(\s*([^)]+)\)", "ToString($1)", RegexOptions.IgnoreCase);

        // Fix NULL usage - replace with proper null checks
        cleanedQuery = Regex.Replace(cleanedQuery, @"\bNULL\b", "null", RegexOptions.IgnoreCase);

        return cleanedQuery.Trim();
    }
}