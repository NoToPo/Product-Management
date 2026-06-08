using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Categories.Dtos;
using ProductManagement.Application.Categories.Mapping;
using ProductManagement.Domain.Common;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Categories.CreateCategory;

public sealed record CreateCategoryCommand(string Name, Guid? ParentId) : IRequest<Result<CategoryResponse>>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateCategoryHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (request.ParentId is { } parentId && !await db.Categories.AnyAsync(c => c.Id == parentId, ct))
            return Error.NotFound("Category.ParentNotFound", "Parent category not found.");

        var result = Category.Create(request.Name, request.ParentId, user.UserId);
        if (result.IsFailure)
            return result.Error;

        var category = result.Value;

        if (await db.Categories.AnyAsync(c => c.Slug == category.Slug, ct))
            return Error.Conflict("Category.SlugExists", $"A category with slug '{category.Slug}' already exists.");

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return CategoryMapper.ToResponse(category);
    }
}
