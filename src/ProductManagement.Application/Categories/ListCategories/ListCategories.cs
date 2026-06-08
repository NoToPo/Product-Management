using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Categories.Dtos;
using ProductManagement.Application.Categories.Mapping;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Categories.ListCategories;

public sealed record ListCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryResponse>>>;

public sealed class ListCategoriesHandler(IAppDbContext db)
    : IRequestHandler<ListCategoriesQuery, Result<IReadOnlyList<CategoryResponse>>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(ListCategoriesQuery request, CancellationToken ct)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        IReadOnlyList<CategoryResponse> response = categories.Select(CategoryMapper.ToResponse).ToList();
        return Result.Success(response);
    }
}
