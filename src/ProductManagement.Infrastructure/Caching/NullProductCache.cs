using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;

namespace ProductManagement.Infrastructure.Caching;

/// <summary>
/// No-op cache used when Redis is not configured, so the API still runs (every read
/// is a cache miss → DB). Keeps local/dev setups dependency-light.
/// </summary>
public sealed class NullProductCache : IProductCache
{
    public Task<ProductResponse?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.FromResult<ProductResponse?>(null);

    public Task SetAsync(ProductResponse product, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
