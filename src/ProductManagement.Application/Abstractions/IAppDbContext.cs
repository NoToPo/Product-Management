using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Abstractions;

/// <summary>
/// Persistence abstraction the Application layer depends on, so handlers stay
/// free of an EF Core dependency on a concrete DbContext. Implemented by
/// Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
