using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Database.Api.Tunnel.Models.Requests;
using Database.Api.Tunnel.Models.Responses;
using Database.Api.Tunnel.Models.Schema;
using Database.Api.Tunnel.Services.Abstractions;
using Database.Api.Tunnel.Utilities;
using Microsoft.Azure.Cosmos;

namespace Database.Api.Tunnel.Services;

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

                    System.Diagnostics.Debug.WriteLine($"Processing container: {containerProps.Id}");

                    // Get multiple recent documents to infer schema (sample last 20 documents for better type inference)
                    var properties = new List<CosmosPropertySchema>();
                    var allDocuments = new List<Dictionary<string, JsonElement>>();

                    try
                    {
                        // First, try to get a simple count to see if there are any documents at all
                        var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                        var countIterator = container.GetItemQueryIterator<int>(countQuery);

                        int documentCount = 0;
                        if (countIterator.HasMoreResults)
                        {
                            var countResponse = await countIterator.ReadNextAsync(cancellationToken);
                            documentCount = countResponse.FirstOrDefault();
                            System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Total document count = {documentCount}");
                            Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Document count = {documentCount}");
                        }

                        // If no documents, skip document retrieval but still log the result
                        if (documentCount == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Container is empty - no documents to analyze");
                            Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Container is empty");
                        }
                        else
                        {
                            Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Found {documentCount} documents, attempting to retrieve samples...");
                        }

                        // Try multiple query approaches to ensure we can get documents
                        var queries = new[]
                        {
                            new QueryDefinition("SELECT TOP 20 * FROM c"),
                            new QueryDefinition("SELECT * FROM c OFFSET 0 LIMIT 20"),
                            new QueryDefinition("SELECT * FROM c")
                        };

                        bool documentsFound = false;

                        foreach (var query in queries)
                        {
                            if (documentsFound) break;

                            try
                            {
                                Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Trying query: {query.QueryText}");
                                var iterator = container.GetItemQueryIterator<dynamic>(query);

                                if (iterator.HasMoreResults)
                                {
                                    var docsResponse = await iterator.ReadNextAsync(cancellationToken);
                                    System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Query '{query.QueryText}' returned {docsResponse.Count} documents");
                                    Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Query returned {docsResponse.Count} documents");

                                    if (docsResponse.Count > 0)
                                    {
                                        documentsFound = true;
                                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Processing {docsResponse.Count} documents...");

                                        foreach (var doc in docsResponse)
                                        {
                                            try
                                            {
                                                // Use the same conversion logic that works for query results
                                                var docDict = CosmosItemConverter.ConvertCosmosItemToDictionary(doc);
                                                System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Successfully processed document with {docDict.Count} properties");

                                                if (docDict.Count > 0)
                                                {
                                                    allDocuments.Add(docDict);
                                                    Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Added document with {docDict.Count} properties to analysis");

                                                    // Log sample properties for debugging
                                                    var sampleProps = docDict.Take(3).ToList();
                                                    foreach (var prop in sampleProps)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Property '{prop.Key}' = {prop.Value.ValueKind} ({prop.Value.GetRawText()})");
                                                        Console.WriteLine($"[COSMOS SCHEMA] Sample property: {prop.Key} = {prop.Value.ValueKind}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Document converted but resulted in empty dictionary");
                                                }
                                            }
                                            catch (Exception docEx)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Error processing individual document: {docEx.Message}");
                                                Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Error processing document: {docEx.Message}");
                                            }
                                        }

                                        // If we got some documents, break out of the query loop
                                        if (allDocuments.Any())
                                        {
                                            Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Successfully collected {allDocuments.Count} documents for analysis");
                                            break;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: No valid documents collected despite query returning results");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Query returned 0 documents");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Query iterator has no results");
                                }
                            }
                            catch (Exception queryEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Query '{query.QueryText}' failed: {queryEx.Message}");
                                Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Query failed: {queryEx.Message}");
                            }
                        }

                        if (!documentsFound)
                        {
                            System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: No documents found with any query method");
                            Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: No documents found with any query method");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: General error during document retrieval: {ex.Message}");
                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: General error: {ex.Message}");
                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Stack trace: {ex.StackTrace}");
                    }

                    Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Final document count for analysis: {allDocuments.Count}");

                    if (allDocuments.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Analyzing {allDocuments.Count} documents");
                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Starting property analysis on {allDocuments.Count} documents");

                        properties = AnalyzePropertyTypesAcrossDocuments(allDocuments);

                        System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Analysis resulted in {properties.Count} properties");
                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: Property analysis completed - found {properties.Count} unique properties");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: No documents to analyze - container appears to be empty");
                        Console.WriteLine($"[COSMOS SCHEMA] Container {containerProps.Id}: No documents to analyze - container appears to be empty or inaccessible");
                        // For empty containers, we still create the container entry but with no properties
                        properties = new List<CosmosPropertySchema>();
                    }

                    // Log the final properties being added to this container
                    System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Adding container to schema with {properties.Count} properties");
                    foreach (var prop in properties.Take(5)) // Log first 5 properties
                    {
                        System.Diagnostics.Debug.WriteLine($"Container {containerProps.Id}: Property '{prop.Name}' (Type: {prop.JsonType}, Nullable: {prop.IsNullable})");
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

            // Enhanced logging for final schema summary
            System.Diagnostics.Debug.WriteLine($"FINAL SCHEMA SUMMARY for database '{request.DatabaseName}':");
            System.Diagnostics.Debug.WriteLine($"Total containers: {containers.Count}");
            var totalProperties = 0;
            foreach (var container in containers)
            {
                var propCount = container.Properties.Count;
                totalProperties += propCount;
                System.Diagnostics.Debug.WriteLine($"  Container '{container.Name}': {propCount} properties, PartitionKey: {container.PartitionKeyPath}");

                // Log sample properties for each container
                foreach (var prop in container.Properties.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"    Property: {prop.Name} ({prop.JsonType}) - Nullable: {prop.IsNullable}");
                }
                if (container.Properties.Count > 3)
                {
                    System.Diagnostics.Debug.WriteLine($"    ... and {container.Properties.Count - 3} more properties");
                }
            }
            System.Diagnostics.Debug.WriteLine($"Total properties across all containers: {totalProperties}");

            // Validate that schema is not null and contains expected data
            if (schema.Schema == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Schema is null!");
                return new CosmosSchemaResponse
                {
                    Success = false,
                    Error = "Schema could not be generated",
                    Schema = null
                };
            }

            if (schema.Schema.Containers == null || schema.Schema.Containers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("WARNING: No containers found in schema");
            }

            System.Diagnostics.Debug.WriteLine($"Schema response created successfully. Success: {schema.Success}");
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
        return GetDetailedJsonType(element, 0);
    }

    /// <summary>
    /// Determines the specific number type (integer, decimal, or float)
    /// </summary>
    /// <param name="element">The JsonElement representing a number</param>
    /// <returns>Specific number type description</returns>
    private static string GetNumberType(JsonElement element)
    {
        if (element.TryGetInt32(out _))
        {
            return "integer";
        }

        if (element.TryGetInt64(out _))
        {
            return "long";
        }

        if (element.TryGetDecimal(out _))
        {
            return "decimal";
        }

        if (element.TryGetDouble(out _))
        {
            return "number";
        }

        return "number"; // fallback
    }

    /// <summary>
    /// Determines the array type by examining array elements
    /// </summary>
    /// <param name="arrayElement">The JsonElement representing an array</param>
    /// <param name="depth">Recursion depth to prevent infinite loops</param>
    /// <returns>Array type description</returns>
    private static string GetArrayType(JsonElement arrayElement, int depth = 0)
    {
        // Prevent infinite recursion with deeply nested arrays
        if (depth > 5)
        {
            return "array";
        }

        var arrayLength = arrayElement.GetArrayLength();

        if (arrayLength == 0)
        {
            return "array"; // Empty array - generic type
        }

        try
        {
            // Analyze multiple elements to determine the most common type
            var elementTypes = new Dictionary<string, int>();
            var elementsToAnalyze = Math.Min(arrayLength, 10); // Analyze up to 10 elements for performance

            var arrayEnumerator = arrayElement.EnumerateArray();
            var elementIndex = 0;

            foreach (var element in arrayEnumerator)
            {
                if (elementIndex >= elementsToAnalyze) break;

                var elementType = GetDetailedJsonType(element, depth + 1);

                // Defensive check: prevent infinite recursion and malformed types
                if (string.IsNullOrEmpty(elementType) || elementType.Contains("array<array<array<"))
                {
                    elementType = "unknown";
                }

                if (elementTypes.ContainsKey(elementType))
                {
                    elementTypes[elementType]++;
                }
                else
                {
                    elementTypes[elementType] = 1;
                }

                elementIndex++;
            }

            // If all elements are the same type, return that type
            if (elementTypes.Count == 1)
            {
                var singleType = elementTypes.Keys.First();
                // Ensure we don't create malformed array types
                if (string.IsNullOrEmpty(singleType) || singleType == "unknown")
                {
                    return "array";
                }
                return $"array<{singleType}>";
            }

            // If mixed types, return the most common one or indicate mixed
            if (elementTypes.Count > 1)
            {
                var validTypes = elementTypes.Where(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Key != "unknown").ToList();

                if (validTypes.Any())
                {
                    var mostCommonType = validTypes.OrderByDescending(kvp => kvp.Value).First().Key;

                    // If we have multiple valid types, create a union type
                    if (validTypes.Count > 1)
                    {
                        var allTypes = validTypes.Select(kvp => kvp.Key).OrderBy(k => k).ToList();
                        return $"array<{string.Join("|", allTypes)}>";
                    }

                    return $"array<{mostCommonType}>";
                }
            }

            return "array"; // fallback
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetArrayType: {ex.Message}");
            return "array";
        }
    }

    /// <summary>
    /// Gets detailed JSON type information from a JsonElement including array element types
    /// </summary>
    /// <param name="element">The JsonElement to analyze</param>
    /// <param name="depth">Recursion depth to prevent infinite loops</param>
    /// <returns>Detailed type description</returns>
    private static string GetDetailedJsonType(JsonElement element, int depth)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => "null",
                JsonValueKind.String => "string",
                JsonValueKind.Number => GetNumberType(element),
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Object => "object",
                JsonValueKind.Array => GetArrayType(element, depth),
                _ => "unknown"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetDetailedJsonType: {ex.Message}");
            return "unknown";
        }
    }

    /// <summary>
    /// Analyzes property types across multiple documents to determine accurate schema
    /// </summary>
    /// <param name="documents">List of documents to analyze</param>
    /// <returns>List of property schemas with accurate types</returns>
    private static List<CosmosPropertySchema> AnalyzePropertyTypesAcrossDocuments(List<Dictionary<string, JsonElement>> documents)
    {
        System.Diagnostics.Debug.WriteLine($"=== ANALYSIS START: Analyzing {documents.Count} documents ===");
        Console.WriteLine($"[COSMOS SCHEMA] === PROPERTY ANALYSIS START: Analyzing {documents.Count} documents ===");

        var propertyTypeMap = new Dictionary<string, Dictionary<string, int>>();
        var propertyPresenceCount = new Dictionary<string, int>();
        var totalDocuments = documents.Count;

        // Collect all property types and their frequency counts
        foreach (var doc in documents)
        {
            Console.WriteLine($"[COSMOS SCHEMA] Processing document with {doc.Count} properties");
            foreach (var prop in doc)
            {
                var propName = prop.Key;
                var propType = GetDetailedJsonType(prop.Value);

                Console.WriteLine($"[COSMOS SCHEMA] Found property: {propName} = {propType}");

                // Track types and their frequencies for this property
                if (!propertyTypeMap.ContainsKey(propName))
                {
                    propertyTypeMap[propName] = new Dictionary<string, int>();
                }

                if (propertyTypeMap[propName].ContainsKey(propType))
                {
                    propertyTypeMap[propName][propType]++;
                }
                else
                {
                    propertyTypeMap[propName][propType] = 1;
                }

                // Track presence count
                if (!propertyPresenceCount.ContainsKey(propName))
                {
                    propertyPresenceCount[propName] = 0;
                }
                propertyPresenceCount[propName]++;
            }
        }

        // Build property schemas
        var properties = new List<CosmosPropertySchema>();
        foreach (var propEntry in propertyTypeMap)
        {
            var propName = propEntry.Key;
            var typeCounts = propEntry.Value;
            var presenceCount = propertyPresenceCount[propName];

            // Determine if property is nullable
            var isNullable = presenceCount < totalDocuments ||
                           typeCounts.ContainsKey("null");

            // Determine the best type representation
            var jsonType = DetermineOptimalType(typeCounts, totalDocuments);

            properties.Add(new CosmosPropertySchema
            {
                Name = propName,
                JsonType = jsonType,
                IsNullable = isNullable
            });
        }

        var orderedProperties = properties.OrderBy(p => p.Name).ToList();
        System.Diagnostics.Debug.WriteLine($"=== ANALYSIS COMPLETE: Found {orderedProperties.Count} properties ===");
        Console.WriteLine($"[COSMOS SCHEMA] === PROPERTY ANALYSIS COMPLETE: Found {orderedProperties.Count} properties ===");

        foreach (var prop in orderedProperties.Take(10)) // Log first 10 properties
        {
            System.Diagnostics.Debug.WriteLine($"ANALYSIS RESULT: Property '{prop.Name}' (Type: {prop.JsonType}, Nullable: {prop.IsNullable})");
            Console.WriteLine($"[COSMOS SCHEMA] Property: {prop.Name} ({prop.JsonType}) - Nullable: {prop.IsNullable}");
        }

        if (orderedProperties.Count > 10)
        {
            Console.WriteLine($"[COSMOS SCHEMA] ... and {orderedProperties.Count - 10} more properties");
        }

        return orderedProperties;
    }

    /// <summary>
    /// Determines the optimal type representation based on type frequency analysis
    /// </summary>
    /// <param name="typeCounts">Dictionary of type names and their occurrence counts</param>
    /// <param name="totalDocuments">Total number of documents analyzed</param>
    /// <returns>Optimal type representation</returns>
    private static string DetermineOptimalType(Dictionary<string, int> typeCounts, int totalDocuments)
    {
        // Remove null from type analysis for primary type determination
        var nonNullTypes = typeCounts.Where(kvp => kvp.Key != "null").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (!nonNullTypes.Any())
        {
            return "null"; // Only null values found
        }

        if (nonNullTypes.Count == 1)
        {
            return nonNullTypes.Keys.First(); // Single consistent type
        }

        // Handle array type consolidation - merge generic "array" with specific array types
        var arrayTypes = nonNullTypes.Where(kvp => kvp.Key.StartsWith("array")).ToList();
        if (arrayTypes.Count > 1)
        {
            // Check if we have both generic "array" and specific array types
            var hasGenericArray = arrayTypes.Any(kvp => kvp.Key == "array");
            var specificArrayTypes = arrayTypes.Where(kvp => kvp.Key != "array").ToList();

            if (hasGenericArray && specificArrayTypes.Any())
            {
                // Use the most specific array type
                var mostFrequentSpecificArray = specificArrayTypes.OrderByDescending(kvp => kvp.Value).First();

                // Remove array types from nonNullTypes and add the consolidated one
                var consolidatedTypes = nonNullTypes.Where(kvp => !kvp.Key.StartsWith("array")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                consolidatedTypes[mostFrequentSpecificArray.Key] = arrayTypes.Sum(kvp => kvp.Value);
                nonNullTypes = consolidatedTypes;
            }
        }

        // Re-check if we now have a single type after consolidation
        if (nonNullTypes.Count == 1)
        {
            return nonNullTypes.Keys.First();
        }

        // Handle multiple types - find the most frequent one
        var mostFrequentType = nonNullTypes.OrderByDescending(kvp => kvp.Value).First();
        var mostFrequentCount = mostFrequentType.Value;

        // If one type represents more than 80% of occurrences, use that type
        var dominanceThreshold = 0.8;
        if ((double)mostFrequentCount / totalDocuments >= dominanceThreshold)
        {
            return mostFrequentType.Key;
        }

        // Handle numeric type consolidation
        var numericTypes = new[] { "integer", "long", "decimal", "number" };
        var hasMultipleNumericTypes = nonNullTypes.Keys.Count(t => numericTypes.Contains(t)) > 1;

        if (hasMultipleNumericTypes)
        {
            // Consolidate to the most general numeric type
            if (nonNullTypes.ContainsKey("decimal") || nonNullTypes.ContainsKey("number"))
                return "number";
            if (nonNullTypes.ContainsKey("long"))
                return "long";
            if (nonNullTypes.ContainsKey("integer"))
                return "integer";
        }

        // For truly mixed types, create a union type
        var sortedTypes = nonNullTypes.Keys.OrderBy(t => t).ToList();
        return string.Join("|", sortedTypes);
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