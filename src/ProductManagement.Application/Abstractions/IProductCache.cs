using ProductManagement.Application.Products.Dtos;

namespace ProductManagement.Application.Abstractions;

/// <summary>
/// Cache-aside store for single-product reads. The cached unit is the read-model
/// DTO (not the entity), so cache hits skip both the DB query and mapping.
/// Writes invalidate the affected product.
/// </summary>
public interface IProductCache
{
    Task<ProductResponse?> GetAsync(Guid productId, CancellationToken cancellationToken = default);
    Task SetAsync(ProductResponse product, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid productId, CancellationToken cancellationToken = default);
}
