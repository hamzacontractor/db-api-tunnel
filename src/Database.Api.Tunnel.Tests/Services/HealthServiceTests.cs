using Database.Api.Tunnel.Models.Responses;
using Database.Api.Tunnel.Services;
using Database.Api.Tunnel.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Database.Api.Tunnel.Tests.Services
{
    /// <summary>
    /// Unit tests for Health Service
    /// </summary>
    public class HealthServiceTests : TestBase
    {
        private readonly Mock<ILogger<HealthService>> _mockLogger;
        private readonly HealthService _healthService;

        public HealthServiceTests()
        {
            _mockLogger = new Mock<ILogger<HealthService>>();
            _healthService = new HealthService();
        }

        [Fact]
        public void CheckHealth_ReturnsHealthyStatus()
        {
            // Act
            var result = _healthService.CheckHealth();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<HealthResponse>();
            result.Status.Should().Be("healthy");
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.Service.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CheckHealth_MultipleCalls_ReturnConsistentService()
        {
            // Act
            var result1 = _healthService.CheckHealth();
            var result2 = _healthService.CheckHealth();

            // Assert
            result1.Service.Should().Be(result2.Service);
        }

        [Fact]
        public void CheckHealth_ReturnsValidTimestamp()
        {
            // Arrange
            var beforeCall = DateTime.UtcNow;

            // Act
            var result = _healthService.CheckHealth();
            var afterCall = DateTime.UtcNow;

            // Assert
            result.Timestamp.Should().BeOnOrAfter(beforeCall);
            result.Timestamp.Should().BeOnOrBefore(afterCall);
        }
    }
}