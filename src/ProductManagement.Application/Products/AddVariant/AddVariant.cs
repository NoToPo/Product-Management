using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.Application.Products.AddVariant;

public sealed record AddVariantCommand(
    Guid ProductId,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    Dictionary<string, object?>? Attributes) : IRequest<Result<VariantResponse>>;

public sealed class AddVariantValidator : AbstractValidator<AddVariantCommand>
{
    public AddVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddVariantHandler(IAppDbContext db, IProductCache cache, ICurrentUser user)
    : IRequestHandler<AddVariantCommand, Result<VariantResponse>>
{
    public async Task<Result<VariantResponse>> Handle(AddVariantCommand request, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product is null)
            return Error.NotFound("Product.NotFound", $"Product '{request.ProductId}' was not found.");

        if (await db.ProductVariants.AnyAsync(v => v.Sku == request.Sku.Trim(), ct))
            return Error.Conflict("Variant.SkuExists", $"SKU '{request.Sku}' is already in use.");

        var priceResult = Money.Create(request.Price, request.Currency);
        if (priceResult.IsFailure)
            return priceResult.Error;

        var variantResult = product.AddVariant(request.Sku, priceResult.Value, request.StockQuantity, request.Attributes, user.UserId);
        if (variantResult.IsFailure)
            return variantResult.Error;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(product.Id, ct);

        return ProductMapper.ToResponse(variantResult.Value);
    }
}
