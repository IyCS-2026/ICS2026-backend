using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class ProductsControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_WhenUnauthenticated_ReturnsOkOrNoContent()
    {
        // Act (AllowAnonymous endpoint)
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Match(s => s == HttpStatusCode.OK || s == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddProduct_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ProductModel.Request("SKU-UNAUTH", "INT-01", "Unauthorized Prod", null, 100m, 5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        // Arrange
        var token = await _factory.GetAdminTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var sku = $"SKU-{Guid.NewGuid():N}";
        var newProduct = new ProductModel.Request(sku, "INT-TEST", "Integration Test Product", "Created via SQLite Test", 299.99m, 15);

        // Act
        var postResponse = await _client.PostAsJsonAsync("/api/products", newProduct);

        // Assert
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await postResponse.Content.ReadFromJsonAsync<ProductModel.ResponseProduct>();
        created.Should().NotBeNull();
        created!.Sku.Should().Be(sku);
        created.Name.Should().Be("Integration Test Product");
        created.CurrentUnitPrice.Should().Be(299.99m);

        // Verify it was actually saved in SQLite database
        await _factory.ExecuteDbContextAsync(async context =>
        {
            var productInDb = await context.Set<Product>().FindAsync(created.Id);
            productInDb.Should().NotBeNull();
            productInDb!.Sku.Should().Be(sku);
            productInDb.CurrentUnitPrice.Should().Be(299.99m);
        });

        // Act: Get by ID
        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductModel.ResponseProduct>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
    }
}
