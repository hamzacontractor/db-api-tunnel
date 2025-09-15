using System.Text.Json;
using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Utilities;
using FluentAssertions;
using Xunit;

namespace DatabaseRag.Api.Tests.Utilities;

/// <summary>
/// Tests for CosmosResultAnalyzer column extraction and row conversion functionality
/// </summary>
public class CosmosResultAnalyzerTests
{
    [Fact]
    public void ExtractColumns_WithEmptyResults_ReturnsEmptyArray()
    {
        var results = new List<Dictionary<string, JsonElement>>();
        
        var columns = CosmosResultAnalyzer.ExtractColumns(results);
        
        columns.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExtractColumns_WithSampleData_ReturnsCorrectColumns()
    {
        var results = new List<Dictionary<string, JsonElement>>
        {
            CreateSampleRow()
        };
        
        var columns = CosmosResultAnalyzer.ExtractColumns(results);
        
        columns.Should().HaveCount(5);
        
        // Verify columns are in deterministic order (system columns first, then alphabetical)
        columns[0].Name.Should().Be("id"); // System column
        columns[1].Name.Should().Be("age"); // Business column (alphabetical)
        columns[2].Name.Should().Be("email"); // Business column
        columns[3].Name.Should().Be("isActive"); // Business column  
        columns[4].Name.Should().Be("name"); // Business column
        
        // Verify column types
        columns.First(c => c.Name == "id").Type.Should().Be("string");
        columns.First(c => c.Name == "name").Type.Should().Be("string");
        columns.First(c => c.Name == "age").Type.Should().Be("number");
        columns.First(c => c.Name == "isActive").Type.Should().Be("boolean");
        columns.First(c => c.Name == "email").Type.Should().Be("null");
    }

    [Fact]
    public void ConvertToStandardRows_WithSampleData_ReturnsCorrectFormat()
    {
        var results = new List<Dictionary<string, JsonElement>>
        {
            CreateSampleRow()
        };
        
        var rows = CosmosResultAnalyzer.ConvertToStandardRows(results);
        
        rows.Should().HaveCount(1);
        var row = rows[0];
        
        row["id"].Should().Be("test-123");
        row["name"].Should().Be("John Doe");
        row["age"].Should().Be(30);
        row["isActive"].Should().Be(true);
        row["email"].Should().BeNull();
    }

    [Fact]
    public void ConvertToStandardRows_WithComplexData_HandlesNestedObjects()
    {
        var complexData = CreateComplexRow();
        var results = new List<Dictionary<string, JsonElement>> { complexData };
        
        var rows = CosmosResultAnalyzer.ConvertToStandardRows(results);
        
        rows.Should().HaveCount(1);
        var row = rows[0];
        
        row["id"].Should().Be("complex-123");
        row["address"].Should().BeOfType<Dictionary<string, object>>();
        row["tags"].Should().BeOfType<object[]>();
        
        var address = (Dictionary<string, object>)row["address"]!;
        address["city"].Should().Be("Seattle");
        address["zipCode"].Should().Be(98101);
        
        var tags = (object[])row["tags"]!;
        tags.Should().HaveCount(2);
        tags[0].Should().Be("premium");
        tags[1].Should().Be("verified");
    }

    [Fact]
    public void CountBusinessColumns_ExcludesSystemColumns()
    {
        var results = new List<Dictionary<string, JsonElement>>
        {
            CreateRowWithSystemColumns()
        };
        
        var businessCount = CosmosResultAnalyzer.CountBusinessColumns(results);
        
        // Should exclude: id, _rid, _etag, _ts (4 system columns)
        // Should include: name, age (2 business columns)
        businessCount.Should().Be(2);
    }

    [Fact]
    public void HasBusinessRelevantData_WithBusinessData_ReturnsTrue()
    {
        var results = new List<Dictionary<string, JsonElement>>
        {
            CreateSampleRow()
        };
        
        var hasBusinessData = CosmosResultAnalyzer.HasBusinessRelevantData(results);
        
        hasBusinessData.Should().BeTrue();
    }

    [Fact]
    public void HasBusinessRelevantData_WithOnlySystemData_ReturnsFalse()
    {
        var systemOnlyRow = new Dictionary<string, JsonElement>
        {
            ["id"] = JsonDocument.Parse("\"test\"").RootElement,
            ["_rid"] = JsonDocument.Parse("\"rid123\"").RootElement,
            ["_etag"] = JsonDocument.Parse("\"etag123\"").RootElement
        };
        
        var results = new List<Dictionary<string, JsonElement>> { systemOnlyRow };
        
        var hasBusinessData = CosmosResultAnalyzer.HasBusinessRelevantData(results);
        
        hasBusinessData.Should().BeFalse();
    }

    private static Dictionary<string, JsonElement> CreateSampleRow()
    {
        return new Dictionary<string, JsonElement>
        {
            ["id"] = JsonDocument.Parse("\"test-123\"").RootElement,
            ["name"] = JsonDocument.Parse("\"John Doe\"").RootElement,
            ["age"] = JsonDocument.Parse("30").RootElement,
            ["isActive"] = JsonDocument.Parse("true").RootElement,
            ["email"] = JsonDocument.Parse("null").RootElement
        };
    }

    private static Dictionary<string, JsonElement> CreateComplexRow()
    {
        var addressJson = """{"city": "Seattle", "zipCode": 98101}""";
        var tagsJson = """["premium", "verified"]""";
        
        return new Dictionary<string, JsonElement>
        {
            ["id"] = JsonDocument.Parse("\"complex-123\"").RootElement,
            ["address"] = JsonDocument.Parse(addressJson).RootElement,
            ["tags"] = JsonDocument.Parse(tagsJson).RootElement
        };
    }

    private static Dictionary<string, JsonElement> CreateRowWithSystemColumns()
    {
        return new Dictionary<string, JsonElement>
        {
            ["id"] = JsonDocument.Parse("\"test-123\"").RootElement,
            ["_rid"] = JsonDocument.Parse("\"rid123\"").RootElement,
            ["_etag"] = JsonDocument.Parse("\"etag123\"").RootElement,
            ["_ts"] = JsonDocument.Parse("1234567890").RootElement,
            ["name"] = JsonDocument.Parse("\"John Doe\"").RootElement,
            ["age"] = JsonDocument.Parse("30").RootElement
        };
    }
}