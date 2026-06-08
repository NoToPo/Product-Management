using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.Application.Products.UpdateVariant;

/// <summary>Updates a variant's price and absolute on-hand stock (not reservations).</summary>
public sealed record UpdateVariantCommand(
    Guid VariantId,
    uint ExpectedVersion,
    decimal Price,
    string Currency,
    int StockQuantity) : IRequest<Result<VariantResponse>>;

public sealed class UpdateVariantValidator : AbstractValidator<UpdateVariantCommand>
{
    public UpdateVariantValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateVariantHandler(IAppDbContext db, IProductCache cache, ICurrentUser user)
    : IRequestHandler<UpdateVariantCommand, Result<VariantResponse>>
{
    public async Task<Result<VariantResponse>> Handle(UpdateVariantCommand request, CancellationToken ct)
    {
        var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.Id == request.VariantId, ct);
        if (variant is null)
            return Error.NotFound("Variant.NotFound", $"Variant '{request.VariantId}' was not found.");

        if (variant.Version != request.ExpectedVersion)
            return ConcurrencyConflict();

        var priceResult = Money.Create(request.Price, request.Currency);
        if (priceResult.IsFailure)
            return priceResult.Error;

        variant.ChangePrice(priceResult.Value, user.UserId);

        var stockResult = variant.SetStock(request.StockQuantity, user.UserId);
        if (stockResult.IsFailure)
            return stockResult.Error;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict();
        }

        await cache.RemoveAsync(variant.ProductId, ct);
        return ProductMapper.ToResponse(variant);
    }

    private static Error ConcurrencyConflict() => Error.Conflict(
        "Variant.ConcurrencyConflict",
        "The variant was modified by someone else. Reload it and retry with the latest ETag.");
}
