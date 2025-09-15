using System.Net;
using System.Text;
using System.Text.Json;
using Database.Api.Tunnel.Models.Requests;
using Database.Api.Tunnel.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Database.Api.Tunnel.Tests.Integration;

/// <summary>
/// Integration tests for CosmosController to verify GenericQueryResponse format
/// </summary>
public class CosmosControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CosmosControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CosmosQuery_WithMissingConnectionString_ReturnsBadRequest()
    {
        // Arrange
        var request = new CosmosQueryRequest("SELECT * FROM c", "TestDb", "TestContainer");
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/cosmos/query", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<object>(content);
        errorResponse.Should().NotBeNull();
    }

    [Fact]
    public async Task CosmosQuery_WithInvalidConnectionString_ReturnsGenericResponse()
    {
        // Arrange  
        var request = new CosmosQueryRequest("SELECT * FROM c", "TestDb", "TestContainer");
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        _client.DefaultRequestHeaders.Add("ConnectionString", "invalid-connection-string");

        // Act
        var response = await _client.PostAsync("/api/cosmos/query", jsonContent);

        // Assert - The new GenericQueryResponse always returns 200 OK with structured response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var queryResponse = JsonSerializer.Deserialize<GenericQueryResponse>(content);

        queryResponse.Should().NotBeNull();
        queryResponse!.Data.Should().NotBeNull();
        queryResponse.Data.Columns.Should().NotBeNull();
        queryResponse.Data.Rows.Should().NotBeNull();

        // Verify we get a valid GenericQueryResponse structure regardless of success/failure
        queryResponse.Description.Should().NotBeNull();
        queryResponse.Conclusion.Should().NotBeNull();
    }

    [Fact]
    public async Task CosmosQuery_ResponseHeaders_ContainCorrectContentType()
    {
        // Arrange
        var request = new CosmosQueryRequest("SELECT * FROM c", "TestDb", "TestContainer");
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        _client.DefaultRequestHeaders.Add("ConnectionString", "invalid-connection-string");

        // Act
        var response = await _client.PostAsync("/api/cosmos/query", jsonContent);

        // Assert - Should be 200 OK with proper content type
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CosmosQuery_WithMissingDatabaseName_ReturnsValidationError()
    {
        // Arrange
        var request = new CosmosQueryRequest("SELECT * FROM c", "", "TestContainer");
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        _client.DefaultRequestHeaders.Add("ConnectionString", "test-connection-string");

        // Act
        var response = await _client.PostAsync("/api/cosmos/query", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("DatabaseName is required");
    }

    [Fact]
    public async Task CosmosQuery_WithMissingContainerName_ReturnsValidationError()
    {
        // Arrange
        var request = new CosmosQueryRequest("SELECT * FROM c", "TestDb", "");
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        _client.DefaultRequestHeaders.Add("ConnectionString", "test-connection-string");

        // Act
        var response = await _client.PostAsync("/api/cosmos/query", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ContainerName is required");
    }

    [Fact]
    public async Task CosmosQuery_WithEmptyQuery_ReturnsValidationError()
    {
        // Arrange
        var request = new CosmosQueryRequest("", "TestDb", "TestContainer");
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        _client.DefaultRequestHeaders.Add("ConnectionString", "test-connection-string");

        // Act
        var response = await _client.PostAsync("/api/cosmos/query", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Query cannot be empty");
    }
}