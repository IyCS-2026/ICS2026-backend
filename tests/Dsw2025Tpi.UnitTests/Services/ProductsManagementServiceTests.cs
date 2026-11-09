using System.Linq.Expressions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dsw2025Tpi.UnitTests.Services;

public class ProductsManagementServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ILogger<ProductsManagementService>> _loggerMock;
    private readonly ProductsManagementService _service;

    public ProductsManagementServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _loggerMock = new Mock<ILogger<ProductsManagementService>>();
        _service = new ProductsManagementService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData("", "Valid Name")]
    [InlineData("   ", "Valid Name")]
    [InlineData("SKU-1", "")]
    [InlineData("SKU-1", "   ")]
    public async Task AddProduct_WhenSkuOrNameIsEmpty_ThrowsInvalidDataException(string sku, string name)
    {
        // Arrange
        var request = new ProductModel.Request(sku, "INT-01", name, "Description", 100m, 10);

        // Act
        var act = () => _service.AddProduct(request);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("El sku y el nombre son requerido");
    }

    [Fact]
    public async Task AddProduct_WhenPriceIsNegative_ThrowsInvalidDataException()
    {
        // Arrange
        var request = new ProductModel.Request("SKU-001", "INT-01", "Product Name", "Description", -5m, 10);

        // Act
        var act = () => _service.AddProduct(request);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("El precio unitario ser mayores a cero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddProduct_WhenStockQuantityIsZeroOrNegative_ThrowsInvalidDataException(int stock)
    {
        // Arrange
        var request = new ProductModel.Request("SKU-001", "INT-01", "Product Name", "Description", 100m, stock);

        // Act
        var act = () => _service.AddProduct(request);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("La cantidad de stock debe ser mayor o igual a cero");
    }

    [Fact]
    public async Task AddProduct_WhenSkuAlreadyExists_ThrowsDuplicatedEntityException()
    {
        // Arrange
        var request = new ProductModel.Request("SKU-EXISTING", "INT-01", "Product Name", "Description", 150m, 5);
        var existingProduct = new Product("SKU-EXISTING", "Existing Product", 150m, 5);

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(existingProduct);

        // Act
        var act = () => _service.AddProduct(request);

        // Assert
        await act.Should().ThrowAsync<DuplicatedEntityException>()
            .WithMessage("*Ya existe un producto con el mismo SKU*");
    }

    [Fact]
    public async Task AddProduct_WhenDataIsValid_ReturnsCreatedProduct()
    {
        // Arrange
        var request = new ProductModel.Request("SKU-NEW", "INT-01", "New Product", "A nice product", 250.50m, 20);

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync((Product?)null);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Act
        var result = await _service.AddProduct(request);

        // Assert
        result.Should().NotBeNull();
        result.Sku.Should().Be("SKU-NEW");
        result.Name.Should().Be("New Product");
        result.CurrentUnitPrice.Should().Be(250.50m);
        result.StockQuantity.Should().Be(20);
        result.IsActive.Should().BeTrue();

        _repositoryMock.Verify(r => r.Add(It.Is<Product>(p => p.Sku == "SKU-NEW" && p.StockQuantity == 20)), Times.Once);
    }

    [Fact]
    public async Task GetProductById_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetById<Product>(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.GetProductById(productId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{productId}*");
    }

    [Fact]
    public async Task GetProductById_WhenProductExists_ReturnsResponseProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("SKU-100", "Monitor", 500m, 3, "Gaming Monitor", "INT-M1")
        {
            Id = productId
        };

        _repositoryMock
            .Setup(r => r.GetById<Product>(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetProductById(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Sku.Should().Be("SKU-100");
        result.Name.Should().Be("Monitor");
    }

    [Fact]
    public async Task DisableProduct_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetById<Product>(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.DisableProduct(productId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task DisableProduct_WhenProductExists_SetsIsActiveToFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("SKU-100", "Keyboard", 100m, 5)
        {
            Id = productId,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetById<Product>(productId))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.Update(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Act
        await _service.DisableProduct(productId);

        // Assert
        product.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.Update(It.Is<Product>(p => p.Id == productId && !p.IsActive)), Times.Once);
    }
}
