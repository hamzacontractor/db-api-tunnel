using System.Text.Json;
using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Models.Schema;

namespace DatabaseRag.Api.Utilities;

/// <summary>
/// Enhanced helper class for analyzing Cosmos DB query results with better JSON format support
/// </summary>
public static class CosmosResultAnalyzer
{
    /// <summary>
    /// Counts the number of business-relevant columns in the result set
    /// </summary>
    /// <param name="results">The query results to analyze</param>
    /// <returns>Number of business columns</returns>
    public static int CountBusinessColumns(IEnumerable<Dictionary<string, object>> results)
    {
        if (!results.Any()) return 0;

        var systemColumns = new HashSet<string> { "id", "_rid", "_self", "_etag", "_attachments", "_ts", "_lsn" };
        var firstItem = results.First();
        return firstItem.Keys.Count(key => !systemColumns.Contains(key.ToLower()));
    }

    /// <summary>
    /// Counts the number of business-relevant columns in the result set (JsonElement version)
    /// </summary>
    /// <param name="results">The query results to analyze with JsonElement values</param>
    /// <returns>Number of business columns</returns>
    public static int CountBusinessColumns(IEnumerable<Dictionary<string, JsonElement>> results)
    {
        if (!results.Any()) return 0;

        var systemColumns = new HashSet<string> { "id", "_rid", "_self", "_etag", "_attachments", "_ts", "_lsn" };
        var firstItem = results.First();
        return firstItem.Keys.Count(key => !systemColumns.Contains(key.ToLower()));
    }

    /// <summary>
    /// Determines if the results contain business-relevant data
    /// </summary>
    /// <param name="results">The query results to analyze</param>
    /// <returns>True if business data is present</returns>
    public static bool HasBusinessRelevantData(IEnumerable<Dictionary<string, object>> results)
    {
        if (!results.Any()) return false;

        var systemColumns = new HashSet<string> { "id", "_rid", "_self", "_etag", "_attachments", "_ts", "_lsn" };
        var firstItem = results.First();
        return firstItem.Keys.Any(key => !systemColumns.Contains(key.ToLower()));
    }

    /// <summary>
    /// Determines if the results contain business-relevant data (JsonElement version)
    /// </summary>
    /// <param name="results">The query results to analyze with JsonElement values</param>
    /// <returns>True if business data is present</returns>
    public static bool HasBusinessRelevantData(IEnumerable<Dictionary<string, JsonElement>> results)
    {
        if (!results.Any()) return false;

        var systemColumns = new HashSet<string> { "id", "_rid", "_self", "_etag", "_attachments", "_ts", "_lsn" };
        var firstItem = results.First();
        return firstItem.Keys.Any(key => !systemColumns.Contains(key.ToLower()));
    }

    /// <summary>
    /// Calculates the complexity level of the schema based on size, structure, and data types
    /// </summary>
    /// <param name="results">The query results to analyze</param>
    /// <returns>Complexity level as a string</returns>
    public static string CalculateSchemaComplexity(IEnumerable<Dictionary<string, object>> results)
    {
        if (!results.Any()) return "Empty";

        var rowCount = results.Count();
        var firstItem = results.First();
        var columnCount = firstItem.Keys.Count;

        // Analyze data types for complexity
        var complexTypes = 0;
        foreach (var value in firstItem.Values)
        {
            if (value is Dictionary<string, object> ||
                value is Array ||
                (value is object[] array && array.Length > 0))
            {
                complexTypes++;
            }
        }

        // Enhanced complexity calculation
        if (columnCount <= 3 && rowCount <= 10 && complexTypes == 0) return "Simple";
        if (columnCount <= 10 && rowCount <= 100 && complexTypes <= 2) return "Moderate";
        if (columnCount <= 20 && rowCount <= 1000 && complexTypes <= 5) return "Complex";
        return "Very Complex";
    }

    /// <summary>
    /// Calculates the complexity level of the schema based on size, structure, and data types (JsonElement version)
    /// </summary>
    /// <param name="results">The query results to analyze with JsonElement values</param>
    /// <returns>Complexity level as a string</returns>
    public static string CalculateSchemaComplexity(IEnumerable<Dictionary<string, JsonElement>> results)
    {
        if (!results.Any()) return "Empty";

        var rowCount = results.Count();
        var firstItem = results.First();
        var columnCount = firstItem.Keys.Count;

        // Analyze data types for complexity
        var complexTypes = 0;
        foreach (var value in firstItem.Values)
        {
            if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
            {
                complexTypes++;
            }
        }

        // Enhanced complexity calculation
        if (columnCount <= 3 && rowCount <= 10 && complexTypes == 0) return "Simple";
        if (columnCount <= 10 && rowCount <= 100 && complexTypes <= 2) return "Moderate";
        if (columnCount <= 20 && rowCount <= 1000 && complexTypes <= 5) return "Complex";
        return "Very Complex";
    }

    /// <summary>
    /// Analyzes the JSON structure quality of the results
    /// </summary>
    /// <param name="results">The query results to analyze</param>
    /// <returns>Quality assessment string</returns>
    public static string AnalyzeJsonQuality(IEnumerable<Dictionary<string, object>> results)
    {
        if (!results.Any()) return "No data";

        var totalProperties = 0;
        var nullProperties = 0;
        var arrayProperties = 0;
        var objectProperties = 0;

        foreach (var result in results.Take(10)) // Sample first 10 items
        {
            foreach (var kvp in result)
            {
                totalProperties++;

                if (kvp.Value == null || kvp.Value == DBNull.Value)
                    nullProperties++;
                else if (kvp.Value is Array)
                    arrayProperties++;
                else if (kvp.Value is Dictionary<string, object>)
                    objectProperties++;
            }
        }

        var nullRatio = (double)nullProperties / totalProperties;
        var complexRatio = (double)(arrayProperties + objectProperties) / totalProperties;

        if (nullRatio > 0.5) return "Poor - High null ratio";
        if (complexRatio > 0.3) return "Rich - Complex structure";
        if (complexRatio > 0.1) return "Good - Some structure";
        return "Basic - Simple types";
    }

    /// <summary>
    /// Extracts columns from Cosmos query results in deterministic order
    /// </summary>
    /// <param name="results">Query results with JsonElement values</param>
    /// <returns>Array of column definitions with consistent ordering</returns>
    public static Column[] ExtractColumns(IEnumerable<Dictionary<string, JsonElement>> results)
    {
        if (!results.Any())
            return Array.Empty<Column>();

        var firstItem = results.First();
        var columns = new List<Column>();

        // Sort columns for deterministic ordering: system columns first, then alphabetical
        var systemColumns = new HashSet<string> { "id", "_rid", "_self", "_etag", "_attachments", "_ts", "_lsn" };
        var sortedKeys = firstItem.Keys.OrderBy(key =>
        {
            if (systemColumns.Contains(key.ToLower()))
                return $"0_{key}"; // System columns first
            return $"1_{key}"; // Business columns alphabetically
        });

        foreach (var key in sortedKeys)
        {
            var value = firstItem[key];
            var column = new Column
            {
                Name = key,
                Type = MapJsonValueKindToColumnType(value.ValueKind),
                IsNullable = IsColumnNullable(key, results)
            };
            columns.Add(column);
        }

        return columns.ToArray();
    }

    /// <summary>
    /// Maps JsonValueKind to standardized column type names
    /// </summary>
    /// <param name="valueKind">The JsonValueKind to map</param>
    /// <returns>Standardized type name</returns>
    private static string MapJsonValueKindToColumnType(JsonValueKind valueKind)
    {
        return valueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            JsonValueKind.Null => "null",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Determines if a column can contain null values by sampling the result set
    /// </summary>
    /// <param name="columnName">Name of the column to check</param>
    /// <param name="results">Query results to sample</param>
    /// <returns>True if column can contain nulls</returns>
    private static bool IsColumnNullable(string columnName, IEnumerable<Dictionary<string, JsonElement>> results)
    {
        // Sample up to 100 rows to check for nulls
        return results.Take(100).Any(row =>
            row.TryGetValue(columnName, out var value) && value.ValueKind == JsonValueKind.Null);
    }

    /// <summary>
    /// Converts JsonElement results to standardized row format with consistent object types
    /// </summary>
    /// <param name="results">Query results with JsonElement values</param>
    /// <returns>Array of row dictionaries with object values</returns>
    public static Dictionary<string, object?>[] ConvertToStandardRows(IEnumerable<Dictionary<string, JsonElement>> results)
    {
        return results.Select(ConvertRowToStandardFormat).ToArray();
    }

    /// <summary>
    /// Converts a single row from JsonElement values to standard object values
    /// </summary>
    /// <param name="row">Row with JsonElement values</param>
    /// <returns>Row with standard object values</returns>
    private static Dictionary<string, object?> ConvertRowToStandardFormat(Dictionary<string, JsonElement> row)
    {
        var standardRow = new Dictionary<string, object?>();

        foreach (var kvp in row)
        {
            standardRow[kvp.Key] = ConvertJsonElementToStandardValue(kvp.Value);
        }

        return standardRow;
    }

    /// <summary>
    /// Converts a JsonElement to appropriate .NET object type for serialization
    /// </summary>
    /// <param name="element">JsonElement to convert</param>
    /// <returns>Standard .NET object</returns>
    private static object? ConvertJsonElementToStandardValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal :
                                   element.TryGetInt64(out var longVal) ? longVal :
                                   element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray()
                                         .Select(ConvertJsonElementToStandardValue)
                                         .ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                                          .ToDictionary(p => p.Name, p => ConvertJsonElementToStandardValue(p.Value)),
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Analyzes the JSON structure quality of the results (JsonElement version)
    /// </summary>
    /// <param name="results">The query results to analyze with JsonElement values</param>
    /// <returns>Quality assessment string</returns>
    public static string AnalyzeJsonQuality(IEnumerable<Dictionary<string, JsonElement>> results)
    {
        if (!results.Any()) return "No data";

        var totalProperties = 0;
        var nullProperties = 0;
        var arrayProperties = 0;
        var objectProperties = 0;

        foreach (var result in results.Take(10)) // Sample first 10 items
        {
            foreach (var kvp in result)
            {
                totalProperties++;

                if (kvp.Value.ValueKind == JsonValueKind.Null)
                    nullProperties++;
                else if (kvp.Value.ValueKind == JsonValueKind.Array)
                    arrayProperties++;
                else if (kvp.Value.ValueKind == JsonValueKind.Object)
                    objectProperties++;
            }
        }

        var nullRatio = (double)nullProperties / totalProperties;
        var complexRatio = (double)(arrayProperties + objectProperties) / totalProperties;

        if (nullRatio > 0.5) return "Poor - High null ratio";
        if (complexRatio > 0.3) return "Rich - Complex structure";
        if (complexRatio > 0.1) return "Good - Some structure";
        return "Basic - Simple types";
    }
}