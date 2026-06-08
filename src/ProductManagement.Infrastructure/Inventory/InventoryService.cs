using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Domain.Common;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure.Inventory;

/// <summary>
/// Concurrency-safe stock operations. Each mutation is a single atomic SQL statement
/// whose WHERE clause encodes the invariant (e.g. "enough available"). The database
/// evaluates the guard and the update as one operation, so two concurrent requests can
/// never both pass the check — no read-modify-write race, no oversell. The check
/// constraints on the table are a final backstop.
/// </summary>
public sealed class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task<Result> ReserveAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Error.Validation("Inventory.InvalidQuantity", "Reserve quantity must be positive.");

        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE product_variants
               SET reserved_quantity = reserved_quantity + {quantity}
             WHERE id = {variantId}
               AND (stock_quantity - reserved_quantity) >= {quantity}
            """,
            cancellationToken);

        if (affected == 1)
            return Result.Success();

        // 0 rows: either the variant is gone or there is not enough available stock.
        var exists = await db.ProductVariants.AnyAsync(v => v.Id == variantId, cancellationToken);
        return exists
            ? Error.Conflict("Inventory.InsufficientStock", "Not enough available stock to reserve the requested quantity.")
            : Error.NotFound("Variant.NotFound", $"Variant '{variantId}' was not found.");
    }

    public async Task<Result> ReleaseAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Error.Validation("Inventory.InvalidQuantity", "Release quantity must be positive.");

        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE product_variants
               SET reserved_quantity = reserved_quantity - {quantity}
             WHERE id = {variantId}
               AND reserved_quantity >= {quantity}
            """,
            cancellationToken);

        if (affected == 1)
            return Result.Success();

        var exists = await db.ProductVariants.AnyAsync(v => v.Id == variantId, cancellationToken);
        return exists
            ? Error.Conflict("Inventory.OverRelease", "Cannot release more than is currently reserved.")
            : Error.NotFound("Variant.NotFound", $"Variant '{variantId}' was not found.");
    }
}
