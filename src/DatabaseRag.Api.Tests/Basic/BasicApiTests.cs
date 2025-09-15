using System.Net;
using System.Net.Http;
using DatabaseRag.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DatabaseRag.Api.Tests.Basic
{
    /// <summary>
    /// Basic API connectivity tests
    /// </summary>
    public class BasicApiTests : TestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BasicApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Health_Endpoint_IsReachable()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task Swagger_IsAccessible()
        {
            // Act
            var response = await _client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SqlSchema_Endpoint_RequiresConnectionString()
        {
            // Act
            var response = await _client.PostAsync("/api/sql/schema",
                new StringContent("", System.Text.Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CosmosSchema_Endpoint_RequiresValidBody()
        {
            // Act
            var response = await _client.PostAsync("/api/cosmos/schema",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("/api/nonexistent")]
        [InlineData("/api/sql/nonexistent")]
        [InlineData("/api/cosmos/nonexistent")]
        public async Task NonExistentEndpoints_Return404(string endpoint)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Api_HandlesInvalidJson_Gracefully()
        {
            // Act
            var response = await _client.PostAsync("/api/sql/query",
                new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        public override void Dispose()
        {
            _client?.Dispose();
            base.Dispose();
        }
    }
}