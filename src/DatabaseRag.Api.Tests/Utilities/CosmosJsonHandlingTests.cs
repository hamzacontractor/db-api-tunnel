using System.Text.Json;
using DatabaseRag.Api.Utilities;
using Xunit;

namespace DatabaseRag.Api.Tests.Utilities;

public class CosmosJsonHandlingTests
{
    [Fact]
    public void JsonElement_CanHandleNestedJsonStructures()
    {
        // This test verifies that our JsonElement-based approach solves the original problem
        // of handling JSON with both string values and nested objects
        var testJsonString = @"{
            ""simpleString"": ""hello"",
            ""simpleNumber"": 42,
            ""simpleBoolean"": true,
            ""simpleNull"": null,
            ""nestedObject"": {
                ""inner"": ""value"", 
                ""count"": 123,
                ""deepNested"": {
                    ""level3"": ""deep value""
                }
            },
            ""arrayData"": [""item1"", ""item2"", {""nested"": true}],
            ""mixedArray"": [1, ""string"", true, null, {""obj"": ""in array""}]
        }";

        // Simulate how API response would be deserialized
        var apiResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(testJsonString);
        Assert.NotNull(apiResponse);

        // Act & Assert - Verify we can read all the types correctly from JsonElement

        // Simple values
        Assert.Equal("hello", apiResponse!["simpleString"].GetString());
        Assert.Equal(42, apiResponse["simpleNumber"].GetInt32());
        Assert.True(apiResponse["simpleBoolean"].GetBoolean());
        Assert.Equal(JsonValueKind.Null, apiResponse["simpleNull"].ValueKind);

        // Nested object should be accessible as JsonElement
        var nestedObj = apiResponse["nestedObject"];
        Assert.Equal(JsonValueKind.Object, nestedObj.ValueKind);
        Assert.Equal("value", nestedObj.GetProperty("inner").GetString());
        Assert.Equal(123, nestedObj.GetProperty("count").GetInt32());

        // Deeply nested object
        var deepNested = nestedObj.GetProperty("deepNested");
        Assert.Equal(JsonValueKind.Object, deepNested.ValueKind);
        Assert.Equal("deep value", deepNested.GetProperty("level3").GetString());

        // Array should be accessible as JsonElement
        var arrayData = apiResponse["arrayData"];
        Assert.Equal(JsonValueKind.Array, arrayData.ValueKind);
        var arrayItems = arrayData.EnumerateArray().ToArray();
        Assert.Equal(3, arrayItems.Length);
        Assert.Equal("item1", arrayItems[0].GetString());
        Assert.Equal("item2", arrayItems[1].GetString());

        var nestedInArray = arrayItems[2];
        Assert.Equal(JsonValueKind.Object, nestedInArray.ValueKind);
        Assert.True(nestedInArray.GetProperty("nested").GetBoolean());

        // Mixed type array
        var mixedArray = apiResponse["mixedArray"];
        Assert.Equal(JsonValueKind.Array, mixedArray.ValueKind);
        var mixedItems = mixedArray.EnumerateArray().ToArray();
        Assert.Equal(5, mixedItems.Length);
        Assert.Equal(1, mixedItems[0].GetInt32());
        Assert.Equal("string", mixedItems[1].GetString());
        Assert.True(mixedItems[2].GetBoolean());
        Assert.Equal(JsonValueKind.Null, mixedItems[3].ValueKind);
        Assert.Equal(JsonValueKind.Object, mixedItems[4].ValueKind);
        Assert.Equal("in array", mixedItems[4].GetProperty("obj").GetString());
    }

    [Fact]
    public void CosmosQueryResponse_CanSerializeJsonElementDictionary()
    {
        // Test that our updated response model can serialize and deserialize correctly
        var sampleData = new List<Dictionary<string, JsonElement>>
        {
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(@"{
                ""id"": ""1"",
                ""name"": ""Test Item"",
                ""metadata"": {
                    ""category"": ""test"",
                    ""score"": 95.5
                },
                ""tags"": [""important"", ""verified""]
            }")!
        };

        var response = new DatabaseRag.Api.Models.Responses.CosmosQueryResponse
        {
            Success = true,
            Data = sampleData
        };

        // Serialize the response
        var json = JsonSerializer.Serialize(response);
        Assert.NotNull(json);
        // Note: .NET 8 serializes property names as PascalCase by default, unless configured otherwise
        Assert.Contains("\"Success\":true", json);
        Assert.Contains("\"data\":", json);

        // Deserialize it back
        var deserialized = JsonSerializer.Deserialize<DatabaseRag.Api.Models.Responses.CosmosQueryResponse>(json);
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Data);
        Assert.Single(deserialized.Data);

        var firstItem = deserialized.Data.First();
        Assert.Equal("1", firstItem["id"].GetString());
        Assert.Equal("Test Item", firstItem["name"].GetString());
        Assert.Equal(JsonValueKind.Object, firstItem["metadata"].ValueKind);
        Assert.Equal(JsonValueKind.Array, firstItem["tags"].ValueKind);
    }
}