using ProductManagement.Application.Common;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Products.Mapping;

/// <summary>Maps domain entities to read-model DTOs (output contract).</summary>
public static class ProductMapper
{
    public static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.Description,
        product.Brand,
        product.Status.ToString(),
        product.CategoryId,
        product.Attributes,
        product.Variants.Select(ToResponse).ToList(),
        ETag.From(product.Version),
        product.CreatedAt,
        product.UpdatedAt);

    public static VariantResponse ToResponse(ProductVariant variant) => new(
        variant.Id,
        variant.Sku,
        variant.Price.Amount,
        variant.Price.Currency,
        variant.StockQuantity,
        variant.ReservedQuantity,
        variant.AvailableQuantity,
        variant.Attributes,
        ETag.From(variant.Version));
}
