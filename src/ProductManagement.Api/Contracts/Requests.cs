namespace ProductManagement.Api.Contracts;

// Request bodies. Route ids and concurrency versions (If-Match) are bound separately,
// so they are intentionally absent here.

public sealed record CreateProductRequest(
    string Name,
    Guid CategoryId,
    string? Description,
    string? Brand,
    Dictionary<string, object?>? Attributes);

public sealed record UpdateProductRequest(
    string Name,
    Guid CategoryId,
    string? Description,
    string? Brand,
    Dictionary<string, object?>? Attributes);

public sealed record AddVariantRequest(
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    Dictionary<string, object?>? Attributes);

public sealed record UpdateVariantRequest(
    decimal Price,
    string Currency,
    int StockQuantity);

public sealed record StockQuantityRequest(int Quantity);

public sealed record CreateCategoryRequest(string Name, Guid? ParentId);
