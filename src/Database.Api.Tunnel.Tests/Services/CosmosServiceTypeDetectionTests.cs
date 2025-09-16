using System.Text.Json;
using Database.Api.Tunnel.Services;
using FluentAssertions;
using Xunit;

namespace Database.Api.Tunnel.Tests.Services;

/// <summary>
/// Tests for CosmosService type detection functionality
/// </summary>
public class CosmosServiceTypeDetectionTests
{
    [Fact]
    public void AnalyzePropertyTypesAcrossDocuments_WithMixedTypes_ReturnsCorrectTypes()
    {
        // Test that the refactored type detection correctly identifies different data types
        // instead of defaulting everything to "array"

        var testDocuments = new List<Dictionary<string, JsonElement>>
        {
            CreateTestDocument("""
            {
                "id": "123",
                "name": "John Doe",
                "age": 25,
                "height": 5.9,
                "isActive": true,
                "tags": ["developer", "manager"],
                "address": {
                    "street": "123 Main St",
                    "city": "Seattle"
                },
                "scores": [95, 87, 92]
            }
            """),
            CreateTestDocument("""
            {
                "id": "456",
                "name": "Jane Smith", 
                "age": 30,
                "height": 5.6,
                "isActive": false,
                "tags": ["designer"],
                "address": {
                    "street": "456 Oak Ave",
                    "city": "Portland"
                },
                "scores": [88, 94, 91, 85]
            }
            """),
            CreateTestDocument("""
            {
                "id": "789",
                "name": "Bob Johnson",
                "age": 35,
                "height": 6.1,
                "isActive": true,
                "tags": [],
                "address": {
                    "street": "789 Pine Rd",
                    "city": "Vancouver"
                },
                "phoneNumber": "555-0123"
            }
            """)
        };

        // Use reflection to access the private method for testing
        var method = typeof(CosmosService).GetMethod("AnalyzePropertyTypesAcrossDocuments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (List<Database.Api.Tunnel.Models.Schema.CosmosPropertySchema>)method!.Invoke(null, new object[] { testDocuments });

        // Verify that different types are correctly detected
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);

        // Check specific property types
        var idProperty = result.FirstOrDefault(p => p.Name == "id");
        idProperty.Should().NotBeNull();
        idProperty!.JsonType.Should().Be("string");
        idProperty.IsNullable.Should().BeFalse();

        var nameProperty = result.FirstOrDefault(p => p.Name == "name");
        nameProperty.Should().NotBeNull();
        nameProperty!.JsonType.Should().Be("string");

        var ageProperty = result.FirstOrDefault(p => p.Name == "age");
        ageProperty.Should().NotBeNull();
        ageProperty!.JsonType.Should().Be("integer");

        var heightProperty = result.FirstOrDefault(p => p.Name == "height");
        heightProperty.Should().NotBeNull();
        heightProperty!.JsonType.Should().Be("decimal"); // Our improved detection identifies this as decimal

        var isActiveProperty = result.FirstOrDefault(p => p.Name == "isActive");
        isActiveProperty.Should().NotBeNull();
        isActiveProperty!.JsonType.Should().Be("boolean");

        var tagsProperty = result.FirstOrDefault(p => p.Name == "tags");
        tagsProperty.Should().NotBeNull();
        tagsProperty!.JsonType.Should().StartWith("array<");

        var addressProperty = result.FirstOrDefault(p => p.Name == "address");
        addressProperty.Should().NotBeNull();
        addressProperty!.JsonType.Should().Be("object");

        var scoresProperty = result.FirstOrDefault(p => p.Name == "scores");
        scoresProperty.Should().NotBeNull();
        scoresProperty!.JsonType.Should().Be("array<integer>");

        // phoneNumber should be nullable since it doesn't appear in all documents
        var phoneProperty = result.FirstOrDefault(p => p.Name == "phoneNumber");
        phoneProperty.Should().NotBeNull();
        phoneProperty!.JsonType.Should().Be("string");
        phoneProperty.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void AnalyzePropertyTypesAcrossDocuments_WithEmptyArray_ReturnsGenericArrayType()
    {
        var testDocuments = new List<Dictionary<string, JsonElement>>
        {
            CreateTestDocument("""
            {
                "id": "123",
                "emptyTags": []
            }
            """)
        };

        var method = typeof(CosmosService).GetMethod("AnalyzePropertyTypesAcrossDocuments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (List<Database.Api.Tunnel.Models.Schema.CosmosPropertySchema>)method!.Invoke(null, new object[] { testDocuments });

        var emptyTagsProperty = result.FirstOrDefault(p => p.Name == "emptyTags");
        emptyTagsProperty.Should().NotBeNull();
        emptyTagsProperty!.JsonType.Should().Be("array");
    }

    [Fact]
    public void AnalyzePropertyTypesAcrossDocuments_WithMixedArrayElementTypes_ReturnsUnionType()
    {
        var testDocuments = new List<Dictionary<string, JsonElement>>
        {
            CreateTestDocument("""
            {
                "id": "123",
                "mixedArray": [1, "text", true]
            }
            """)
        };

        var method = typeof(CosmosService).GetMethod("AnalyzePropertyTypesAcrossDocuments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (List<Database.Api.Tunnel.Models.Schema.CosmosPropertySchema>)method!.Invoke(null, new object[] { testDocuments });

        var mixedArrayProperty = result.FirstOrDefault(p => p.Name == "mixedArray");
        mixedArrayProperty.Should().NotBeNull();
        mixedArrayProperty!.JsonType.Should().Contain("array<");
        mixedArrayProperty.JsonType.Should().Contain("|"); // Should be a union type
    }

    private static Dictionary<string, JsonElement> CreateTestDocument(string json)
    {
        var jsonDocument = JsonDocument.Parse(json);
        var result = new Dictionary<string, JsonElement>();

        foreach (var property in jsonDocument.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.Clone();
        }

        return result;
    }
}