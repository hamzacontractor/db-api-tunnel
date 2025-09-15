using System.Text.Json;

namespace Database.Api.Tunnel.Utilities;

/// <summary>
/// Utility class for converting Cosmos DB items to dictionaries
/// </summary>
public static class CosmosItemConverter
{
    /// <summary>
    /// Converts a Cosmos DB dynamic item to a dictionary with enhanced JSON formatting
    /// </summary>
    /// <param name="item">The dynamic item from Cosmos DB SDK</param>
    /// <returns>Dictionary representation of the item using JsonElement to preserve structure</returns>
    public static Dictionary<string, JsonElement> ConvertCosmosItemToDictionary(dynamic item)
    {
        // The issue is that JsonSerializer.Serialize() doesn't handle Cosmos SDK dynamic objects properly
        // Instead, let's try to convert it more directly

        try
        {
            // First, check if we can convert it to a string representation
            var itemType = item.GetType();
            Console.WriteLine($"[DEBUG] Item type: {itemType.Name}");

            // Try to see if the item has a ToString method that gives us JSON
            var jsonString = item.ToString();
            Console.WriteLine($"[DEBUG] Item ToString: {jsonString}");

            // If ToString gives us valid JSON, use that
            if (jsonString.StartsWith("{") && jsonString.EndsWith("}"))
            {
                var directJsonDoc = JsonDocument.Parse(jsonString);
                var directRoot = directJsonDoc.RootElement;

                if (directRoot.ValueKind == JsonValueKind.Object)
                {
                    var directResult = ConvertJsonElementToDictionary(directRoot);

                    // Clone all JsonElements to make them independent
                    var directClonedResult = new Dictionary<string, JsonElement>();
                    foreach (var kvp in directResult)
                    {
                        directClonedResult[kvp.Key] = kvp.Value.Clone();
                    }
                    directJsonDoc.Dispose();
                    return directClonedResult;
                }
                directJsonDoc.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Direct conversion failed: {ex.Message}");
        }

        // Fallback: Serialize the SDK item to JSON to have a stable representation
        var json = JsonSerializer.Serialize(item);
        Console.WriteLine($"[DEBUG] Fallback serialized JSON: {json}");

        // Parse the JSON and create a new JsonDocument that we own and control disposal of
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Enhanced debugging for root element
        Console.WriteLine($"[DEBUG] Root element type: {root.ValueKind}");

        Dictionary<string, JsonElement> result;

        // If root is an object, convert properties -> dictionary
        if (root.ValueKind == JsonValueKind.Object)
        {
            result = ConvertJsonElementToDictionary(root);
            Console.WriteLine($"[DEBUG] Converted dictionary has {result.Count} properties");
            // Log first 3 properties using proper enumeration
            var count = 0;
            foreach (var kvp in result)
            {
                if (count >= 3) break;
                Console.WriteLine($"[DEBUG] Property '{kvp.Key}': {kvp.Value.ValueKind} = {kvp.Value.GetRawText()}");
                count++;
            }
        }
        // If root is an array, convert each item and return as a single dictionary entry
        else if (root.ValueKind == JsonValueKind.Array)
        {
            result = new Dictionary<string, JsonElement> { ["_array"] = root.Clone() };
        }
        // If primitive (string/number/bool/null), return as single value under key "_value"
        else
        {
            result = new Dictionary<string, JsonElement> { ["_value"] = root.Clone() };
        }

        // Clone all JsonElements to make them independent of the original JsonDocument
        var clonedResult = new Dictionary<string, JsonElement>();
        foreach (var kvp in result)
        {
            clonedResult[kvp.Key] = kvp.Value.Clone();
        }

        // Now we can safely dispose the document
        doc.Dispose();

        return clonedResult;
    }

    /// <summary>
    /// Converts a JsonElement to a dictionary preserving the JSON structure
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>Dictionary representation with JsonElements preserved</returns>
    public static Dictionary<string, JsonElement> ConvertJsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, JsonElement>();

        Console.WriteLine($"[DEBUG] Converting object with {element.EnumerateObject().Count()} properties");

        foreach (var property in element.EnumerateObject())
        {
            // Clone the JsonElement to make it independent of the original JsonDocument
            result[property.Name] = property.Value.Clone();

            // Enhanced debugging for all properties
            Console.WriteLine($"[DEBUG] Property '{property.Name}' (type: {property.Value.ValueKind}): {property.Value.GetRawText()}");
        }

        Console.WriteLine($"[DEBUG] Dictionary conversion complete. Result has {result.Count} properties");
        return result;
    }

    /// <summary>
    /// Converts a JsonElement to an appropriate object type
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>Converted object</returns>
    public static object ConvertJsonElementToObject(JsonElement element)
    {
        // Enhanced debugging for each element conversion
        Console.WriteLine($"[DEBUG] Converting JsonElement of type: {element.ValueKind}, Raw value: {element.GetRawText()}");

        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                Console.WriteLine($"[DEBUG] Null value detected");
                return null!; // Return null instead of DBNull.Value for better JSON serialization

            case JsonValueKind.True:
                Console.WriteLine($"[DEBUG] Boolean true detected");
                return true;

            case JsonValueKind.False:
                Console.WriteLine($"[DEBUG] Boolean false detected");
                return false;

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                {
                    Console.WriteLine($"[DEBUG] Integer value: {intValue}");
                    return intValue;
                }
                if (element.TryGetInt64(out var longValue))
                {
                    Console.WriteLine($"[DEBUG] Long value: {longValue}");
                    return longValue;
                }
                var doubleValue = element.GetDouble();
                Console.WriteLine($"[DEBUG] Double value: {doubleValue}");
                return doubleValue;

            case JsonValueKind.String:
                var stringValue = element.GetString() ?? string.Empty;
                Console.WriteLine($"[DEBUG] String value: '{stringValue}'");
                return stringValue;

            case JsonValueKind.Array:
                Console.WriteLine($"[DEBUG] Converting array with {element.GetArrayLength()} items");
                var arrayItems = element.EnumerateArray().Select(ConvertJsonElementToObject).ToArray();
                Console.WriteLine($"[DEBUG] Array conversion resulted in {arrayItems.Length} items");

                // Preserve arrays as-is instead of converting empty arrays to null
                // This ensures proper JSON structure is maintained
                if (arrayItems.Length > 0)
                {
                    Console.WriteLine($"[DEBUG] Array first item: {arrayItems[0]?.ToString() ?? "null"}");
                }
                return arrayItems;

            case JsonValueKind.Object:
                Console.WriteLine($"[DEBUG] Converting object");
                return ConvertJsonElementToDictionary(element);

            case JsonValueKind.Undefined:
                Console.WriteLine($"[DEBUG] Undefined value detected, returning null");
                return null!;

            default:
                Console.WriteLine($"[DEBUG] Unknown type, returning string representation: {element.GetRawText()}");
                return element.GetRawText();
        }
    }

    /// <summary>
    /// Infers the JSON type from a JsonElement
    /// </summary>
    /// <param name="element">The JsonElement to analyze</param>
    /// <returns>String representation of the JSON type</returns>
    public static string InferJsonTypeFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "null",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            _ => "unknown"
        };
    }
}