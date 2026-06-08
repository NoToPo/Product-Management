namespace ProductManagement.Application.Products.Dtos;

/// <summary>Read-model for a single SKU/variant. A stable output contract that
/// never exposes the underlying entity or its concurrency internals.</summary>
public sealed record VariantResponse(
    Guid Id,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    IReadOnlyDictionary<string, object?> Attributes,
    string ETag);
