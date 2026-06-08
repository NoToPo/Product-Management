using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    uint ExpectedVersion,
    string Name,
    Guid CategoryId,
    string? Description,
    string? Brand,
    Dictionary<string, object?>? Attributes) : IRequest<Result<ProductResponse>>;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Brand).MaximumLength(200);
    }
}

public sealed class UpdateProductHandler(IAppDbContext db, IProductCache cache, ICurrentUser user)
    : IRequestHandler<UpdateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Error.NotFound("Product.NotFound", $"Product '{request.Id}' was not found.");

        // Optimistic concurrency (fast path): reject a stale client up front.
        if (product.Version != request.ExpectedVersion)
            return ConcurrencyConflict();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            return Error.NotFound("Product.CategoryNotFound", "The specified category does not exist.");

        var newSlug = SlugGenerator.Generate(request.Name);
        if (await db.Products.IgnoreQueryFilters()
                .AnyAsync(p => p.Slug == newSlug && p.Id != product.Id && !p.IsDeleted, ct))
            return Error.Conflict("Product.SlugExists", $"A product with slug '{newSlug}' already exists.");

        var update = product.UpdateDetails(request.Name, request.CategoryId, request.Description, request.Brand, request.Attributes, user.UserId);
        if (update.IsFailure)
            return update.Error;

        try
        {
            // Safety net: the xmin token guards against a concurrent writer between load and save.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict();
        }

        await cache.RemoveAsync(product.Id, ct);
        return ProductMapper.ToResponse(product);
    }

    private static Error ConcurrencyConflict() => Error.Conflict(
        "Product.ConcurrencyConflict",
        "The product was modified by someone else. Reload it and retry with the latest ETag.");
}
