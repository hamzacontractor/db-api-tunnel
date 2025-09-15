using System.Text.Json;
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