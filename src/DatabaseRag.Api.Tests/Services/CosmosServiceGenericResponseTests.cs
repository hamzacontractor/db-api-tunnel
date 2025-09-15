using DatabaseRag.Api.Models.Requests;
using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Services;
using DatabaseRag.Api.Services.Abstractions;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatabaseRag.Api.Tests.Services;

/// <summary>
/// Tests for CosmosService GenericQueryResponse mapping functionality
/// </summary>
public class CosmosServiceGenericResponseTests
{
    [Fact]
    public async Task ExecuteQueryAsGenericAsync_WithEmptyConnectionString_ReturnsErrorResponse()
    {
        // This test validates that the new GenericQueryResponse mapping works correctly
        // with invalid connection strings - should return error response instead of throwing
        
        var service = new CosmosService();
        var request = new CosmosQueryRequest("SELECT * FROM c", "TestDb", "TestContainer");
        
        var result = await service.ExecuteQueryAsGenericAsync(request, "", CancellationToken.None);
        
        // Verify the method returns an error response with proper structure
        result.Should().NotBeNull();
        result.Description.Should().Contain("failed");
        result.Data.Should().NotBeNull();
        result.Data.Columns.Should().BeEmpty();
        result.Data.Rows.Should().BeEmpty();
        result.Conclusion.Should().Contain("failed");
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Source.Should().Be("Cosmos DB");
    }

    [Fact]
    public void GenericQueryResponse_Structure_IsCorrect()
    {
        // Test the structure of GenericQueryResponse to ensure it meets requirements
        var response = new GenericQueryResponse
        {
            Description = "Test description",
            Data = new QueryData
            {
                Columns = new[]
                {
                    new Column { Name = "id", Type = "string", IsNullable = false },
                    new Column { Name = "name", Type = "string", IsNullable = true }
                },
                Rows = new[]
                {
                    new Dictionary<string, object?> { ["id"] = "1", ["name"] = "Test" },
                    new Dictionary<string, object?> { ["id"] = "2", ["name"] = null }
                }
            },
            Conclusion = "Test conclusion",
            Metadata = new Meta
            {
                Source = "Cosmos DB",
                Properties = new Dictionary<string, object?> { ["test"] = "value" }
            }
        };

        // Verify all required properties exist
        response.Description.Should().Be("Test description");
        response.Data.Should().NotBeNull();
        response.Data.Columns.Should().HaveCount(2);
        response.Data.Rows.Should().HaveCount(2);
        response.Conclusion.Should().Be("Test conclusion");
        response.Metadata.Should().NotBeNull();
        response.Metadata!.Source.Should().Be("Cosmos DB");
        
        // Verify column structure
        var idColumn = response.Data.Columns[0];
        idColumn.Name.Should().Be("id");
        idColumn.Type.Should().Be("string");
        idColumn.IsNullable.Should().BeFalse();
        
        // Verify row structure
        var firstRow = response.Data.Rows[0];
        firstRow["id"].Should().Be("1");
        firstRow["name"].Should().Be("Test");
        
        var secondRow = response.Data.Rows[1];
        secondRow["id"].Should().Be("2");
        secondRow["name"].Should().BeNull();
    }

    [Fact]
    public void QueryData_DefaultValues_AreCorrect()
    {
        var queryData = new QueryData();
        
        queryData.Columns.Should().NotBeNull().And.BeEmpty();
        queryData.Rows.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Column_Properties_WorkCorrectly()
    {
        var column = new Column
        {
            Name = "testColumn",
            Type = "number",
            IsNullable = true
        };
        
        column.Name.Should().Be("testColumn");
        column.Type.Should().Be("number");
        column.IsNullable.Should().BeTrue();
    }
}