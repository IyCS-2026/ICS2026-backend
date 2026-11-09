using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using FluentAssertions;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class AuthenticateControllerTests
{
    private readonly HttpClient _client;

    public AuthenticateControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsToken()
    {
        // Arrange (Admin is seeded in SQLite AuthenticateContext via admins.json)
        var login = new LoginModel("admin", "Admin123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("token");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var login = new LoginModel("admin", "WrongPassword!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
