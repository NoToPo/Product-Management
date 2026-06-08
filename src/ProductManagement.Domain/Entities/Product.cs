using ProductManagement.Domain.Common;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.Domain.Entities;

/// <summary>
/// Aggregate root for the catalogue. A product groups one or more
/// <see cref="ProductVariant"/> SKUs and carries schema-less <see cref="Attributes"/>
/// (JSONB) so new merchandising fields can be added without a migration.
/// Deletion is soft (<see cref="IsDeleted"/>) to preserve order/history references.
/// </summary>
public sealed class Product : Entity
{
    private readonly List<ProductVariant> _variants = [];
    private Dictionary<string, object?> _attributes = new();

    private Product() { } // EF Core

    private Product(Guid id, string name, string slug, string? description, string? brand, Guid categoryId)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        Brand = brand;
        CategoryId = categoryId;
        Status = ProductStatus.Draft;
    }

    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Brand { get; private set; }
    public ProductStatus Status { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;
    public IReadOnlyList<ProductVariant> Variants => _variants;

    public static Result<Product> Create(
        string name,
        Guid categoryId,
        string? description = null,
        string? brand = null,
        IReadOnlyDictionary<string, object?>? attributes = null,
        string? by = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Product.NameRequired", "Product name is required.");

        if (categoryId == Guid.Empty)
            return Error.Validation("Product.CategoryRequired", "A product must belong to a category.");

        var slug = SlugGenerator.Generate(name);
        if (slug.Length == 0)
            return Error.Validation("Product.InvalidName", "Product name must contain alphanumeric characters.");

        var product = new Product(Guid.CreateVersion7(), name.Trim(), slug, description?.Trim(), brand?.Trim(), categoryId);
        product.ReplaceAttributes(attributes);
        product.Stamp(by);
        return product;
    }

    public Result UpdateDetails(
        string name,
        Guid categoryId,
        string? description,
        string? brand,
        IReadOnlyDictionary<string, object?>? attributes,
        string? by = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Product.NameRequired", "Product name is required.");

        if (categoryId == Guid.Empty)
            return Error.Validation("Product.CategoryRequired", "A product must belong to a category.");

        var slug = SlugGenerator.Generate(name);
        if (slug.Length == 0)
            return Error.Validation("Product.InvalidName", "Product name must contain alphanumeric characters.");

        Name = name.Trim();
        Slug = slug;
        CategoryId = categoryId;
        Description = description?.Trim();
        Brand = brand?.Trim();
        ReplaceAttributes(attributes);
        Touch(by);
        return Result.Success();
    }

    public Result ChangeStatus(ProductStatus status, string? by = null)
    {
        if (status == ProductStatus.Active && _variants.Count == 0)
            return Error.Conflict("Product.NoVariants", "A product needs at least one variant before it can be activated.");

        Status = status;
        Touch(by);
        return Result.Success();
    }

    public Result<ProductVariant> AddVariant(
        string sku,
        Money price,
        int stockQuantity,
        IReadOnlyDictionary<string, object?>? attributes = null,
        string? by = null)
    {
        var variantResult = ProductVariant.Create(Id, sku, price, stockQuantity, attributes, by);
        if (variantResult.IsFailure)
            return variantResult;

        _variants.Add(variantResult.Value);
        Touch(by);
        return variantResult;
    }

    public void SoftDelete(string? by = null)
    {
        IsDeleted = true;
        Touch(by);
    }

    private void ReplaceAttributes(IReadOnlyDictionary<string, object?>? attributes)
    {
        _attributes = attributes is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(attributes);
    }
}
