using System.Net;
using FluentAssertions;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests;

[Collection("IntegrationTests")]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyOk()
    {
        // Act
        var response = await _client.GetAsync("/healthcheck");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
}
