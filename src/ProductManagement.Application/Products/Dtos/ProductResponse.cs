namespace ProductManagement.Application.Products.Dtos;

/// <summary>Read-model for a product, including its variants. <see cref="ETag"/>
/// carries the concurrency token clients echo back via <c>If-Match</c>.</summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Brand,
    string Status,
    Guid CategoryId,
    IReadOnlyDictionary<string, object?> Attributes,
    IReadOnlyList<VariantResponse> Variants,
    string ETag,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
