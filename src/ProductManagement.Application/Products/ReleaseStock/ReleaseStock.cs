using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Products.ReleaseStock;

/// <summary>Releases a previously held reservation (e.g. cancelled/expired order).</summary>
public sealed record ReleaseStockCommand(Guid VariantId, int Quantity) : IRequest<Result<VariantResponse>>;

public sealed class ReleaseStockValidator : AbstractValidator<ReleaseStockCommand>
{
    public ReleaseStockValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class ReleaseStockHandler(IInventoryService inventory, IAppDbContext db, IProductCache cache)
    : IRequestHandler<ReleaseStockCommand, Result<VariantResponse>>
{
    public async Task<Result<VariantResponse>> Handle(ReleaseStockCommand request, CancellationToken ct)
    {
        var release = await inventory.ReleaseAsync(request.VariantId, request.Quantity, ct);
        if (release.IsFailure)
            return release.Error;

        var variant = await db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(v => v.Id == request.VariantId, ct);
        if (variant is null)
            return Error.NotFound("Variant.NotFound", $"Variant '{request.VariantId}' was not found.");

        await cache.RemoveAsync(variant.ProductId, ct);
        return ProductMapper.ToResponse(variant);
    }
}
