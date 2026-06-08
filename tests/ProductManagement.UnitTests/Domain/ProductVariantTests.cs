using FluentAssertions;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.UnitTests.Domain;

public class ProductVariantTests
{
    private static ProductVariant NewVariant(int stock = 10)
    {
        var price = Money.Create(50m, "USD").Value;
        return ProductVariant.Create(Guid.NewGuid(), "SKU-1", price, stock).Value;
    }

    [Fact]
    public void Create_WithNegativeStock_Fails()
    {
        var price = Money.Create(50m, "USD").Value;
        var result = ProductVariant.Create(Guid.NewGuid(), "SKU-1", price, -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Variant.NegativeStock");
    }

    [Fact]
    public void Reserve_WithinAvailable_DecrementsAvailability()
    {
        var variant = NewVariant(stock: 10);

        var result = variant.Reserve(4);

        result.IsSuccess.Should().BeTrue();
        variant.ReservedQuantity.Should().Be(4);
        variant.AvailableQuantity.Should().Be(6);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_Fails()
    {
        var variant = NewVariant(stock: 3);

        var result = variant.Reserve(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Variant.InsufficientStock");
    }

    [Fact]
    public void SetStock_BelowReserved_Fails()
    {
        var variant = NewVariant(stock: 10);
        variant.Reserve(8);

        var result = variant.SetStock(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Variant.StockBelowReserved");
    }

    [Fact]
    public void Release_MoreThanReserved_Fails()
    {
        var variant = NewVariant(stock: 10);
        variant.Reserve(2);

        var result = variant.Release(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Variant.OverRelease");
    }
}
