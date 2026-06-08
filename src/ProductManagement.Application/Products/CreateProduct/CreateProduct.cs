using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    Guid CategoryId,
    string? Description,
    string? Brand,
    Dictionary<string, object?>? Attributes) : IRequest<Result<ProductResponse>>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Brand).MaximumLength(200);
    }
}

public sealed class CreateProductHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            return Error.NotFound("Product.CategoryNotFound", "The specified category does not exist.");

        var result = Product.Create(request.Name, request.CategoryId, request.Description, request.Brand, request.Attributes, user.UserId);
        if (result.IsFailure)
            return result.Error;

        var product = result.Value;

        if (await db.Products.IgnoreQueryFilters().AnyAsync(p => p.Slug == product.Slug && !p.IsDeleted, ct))
            return Error.Conflict("Product.SlugExists", $"A product with slug '{product.Slug}' already exists.");

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return ProductMapper.ToResponse(product);
    }
}
