using FluentAssertions;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_StartsAsDraftWithSlug()
    {
        var result = Product.Create("Áo Khoác Denim", Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ProductStatus.Draft);
        result.Value.Slug.Should().Be("ao-khoac-denim"); // diacritics stripped, spaced -> hyphen
    }

    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        var result = Product.Create("   ", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NameRequired");
    }

    [Fact]
    public void ChangeStatus_ToActiveWithoutVariants_Fails()
    {
        var product = Product.Create("Tee", Guid.NewGuid()).Value;

        var result = product.ChangeStatus(ProductStatus.Active);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NoVariants");
    }

    [Fact]
    public void AddVariant_ThenActivate_Succeeds()
    {
        var product = Product.Create("Tee", Guid.NewGuid()).Value;
        var price = Money.Create(19.50m, "USD").Value;

        product.AddVariant("TEE-M", price, 100);
        var activation = product.ChangeStatus(ProductStatus.Active);

        activation.IsSuccess.Should().BeTrue();
        product.Variants.Should().HaveCount(1);
        product.Status.Should().Be(ProductStatus.Active);
    }
}
