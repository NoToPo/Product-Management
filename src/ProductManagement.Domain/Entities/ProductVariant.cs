using ProductManagement.Domain.Common;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.Domain.Entities;

/// <summary>
/// A sellable unit of a <see cref="Product"/> (e.g. "Red / M"). Owns its own SKU,
/// price and stock. Stock is split into <see cref="StockQuantity"/> (on hand) and
/// <see cref="ReservedQuantity"/> (held by in-flight orders); the difference is the
/// quantity actually available to sell — the basis for oversell prevention.
/// </summary>
public sealed class ProductVariant : Entity
{
    private Dictionary<string, object?> _attributes = new();

    private ProductVariant() { } // EF Core

    private ProductVariant(Guid id, Guid productId, string sku, Money price, int stockQuantity)
    {
        Id = id;
        ProductId = productId;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        ReservedQuantity = 0;
    }

    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    /// <summary>On-hand minus reserved — what a new order may still claim.</summary>
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    /// <summary>Free-form, schema-less attributes (size, color, …) persisted as JSONB.</summary>
    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public static Result<ProductVariant> Create(
        Guid productId,
        string sku,
        Money price,
        int stockQuantity,
        IReadOnlyDictionary<string, object?>? attributes = null,
        string? by = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return Error.Validation("Variant.SkuRequired", "SKU is required.");

        if (stockQuantity < 0)
            return Error.Validation("Variant.NegativeStock", "Stock quantity cannot be negative.");

        var variant = new ProductVariant(Guid.CreateVersion7(), productId, sku.Trim(), price, stockQuantity);
        variant.ReplaceAttributes(attributes);
        variant.Stamp(by);
        return variant;
    }

    public Result ChangePrice(Money price, string? by = null)
    {
        Price = price;
        Touch(by);
        return Result.Success();
    }

    /// <summary>Sets the absolute on-hand quantity. Cannot drop below what is reserved.</summary>
    public Result SetStock(int stockQuantity, string? by = null)
    {
        if (stockQuantity < 0)
            return Error.Validation("Variant.NegativeStock", "Stock quantity cannot be negative.");

        if (stockQuantity < ReservedQuantity)
            return Error.Conflict(
                "Variant.StockBelowReserved",
                $"On-hand stock ({stockQuantity}) cannot be lower than reserved ({ReservedQuantity}).");

        StockQuantity = stockQuantity;
        Touch(by);
        return Result.Success();
    }

    /// <summary>
    /// Reserves <paramref name="quantity"/> units if available. This is the in-memory
    /// guard; the infrastructure layer additionally enforces it atomically in SQL to be
    /// safe under concurrency.
    /// </summary>
    public Result Reserve(int quantity, string? by = null)
    {
        if (quantity <= 0)
            return Error.Validation("Variant.InvalidQuantity", "Reserve quantity must be positive.");

        if (quantity > AvailableQuantity)
            return Error.Conflict(
                "Variant.InsufficientStock",
                $"Only {AvailableQuantity} unit(s) available; cannot reserve {quantity}.");

        ReservedQuantity += quantity;
        Touch(by);
        return Result.Success();
    }

    /// <summary>Releases a previous reservation (e.g. cancelled order).</summary>
    public Result Release(int quantity, string? by = null)
    {
        if (quantity <= 0)
            return Error.Validation("Variant.InvalidQuantity", "Release quantity must be positive.");

        if (quantity > ReservedQuantity)
            return Error.Validation("Variant.OverRelease", "Cannot release more than is reserved.");

        ReservedQuantity -= quantity;
        Touch(by);
        return Result.Success();
    }

    private void ReplaceAttributes(IReadOnlyDictionary<string, object?>? attributes)
    {
        _attributes = attributes is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(attributes);
    }
}
