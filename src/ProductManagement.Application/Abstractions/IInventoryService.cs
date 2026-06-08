using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Abstractions;

/// <summary>
/// Stock movements that must be safe under concurrency. The implementation performs
/// an atomic, guarded SQL update (single round-trip) so two simultaneous orders can
/// never both succeed past the available quantity — preventing oversell without a
/// read-modify-write race.
/// </summary>
public interface IInventoryService
{
    /// <summary>Atomically reserves stock if (on-hand − reserved) ≥ quantity.</summary>
    Task<Result> ReserveAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Atomically releases a previously held reservation.</summary>
    Task<Result> ReleaseAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
}
