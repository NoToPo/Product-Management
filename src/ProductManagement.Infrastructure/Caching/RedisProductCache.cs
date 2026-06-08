using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;

namespace ProductManagement.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache-aside store for single-product reads. Bounded TTL keeps entries
/// fresh even if an invalidation is ever missed; writes proactively evict the key.
/// </summary>
public sealed class RedisProductCache(IDistributedCache cache) : IProductCache
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    private static string Key(Guid productId) => $"product:{productId}";

    public async Task<ProductResponse?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(Key(productId), cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<ProductResponse>(bytes, Options);
    }

    public Task SetAsync(ProductResponse product, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(product, Options);
        return cache.SetAsync(Key(product.Id), bytes, EntryOptions, cancellationToken);
    }

    public Task RemoveAsync(Guid productId, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(Key(productId), cancellationToken);
}
