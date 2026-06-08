using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Products.ReserveStock;

/// <summary>
/// Reserves stock for a variant. Delegates the mutation to <see cref="IInventoryService"/>,
/// which performs an atomic, oversell-safe SQL update.
/// </summary>
public sealed record ReserveStockCommand(Guid VariantId, int Quantity) : IRequest<Result<VariantResponse>>;

public sealed class ReserveStockValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class ReserveStockHandler(IInventoryService inventory, IAppDbContext db, IProductCache cache)
    : IRequestHandler<ReserveStockCommand, Result<VariantResponse>>
{
    public async Task<Result<VariantResponse>> Handle(ReserveStockCommand request, CancellationToken ct)
    {
        var reservation = await inventory.ReserveAsync(request.VariantId, request.Quantity, ct);
        if (reservation.IsFailure)
            return reservation.Error;

        var variant = await db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v => v.Id == request.VariantId, ct);
        if (variant is null)
            return Error.NotFound("Variant.NotFound", $"Variant '{request.VariantId}' was not found.");

        await cache.RemoveAsync(variant.ProductId, ct);
        return ProductMapper.ToResponse(variant);
    }
}
