using ProductManagement.Application.Categories.Dtos;
using ProductManagement.Application.Common;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Categories.Mapping;

public static class CategoryMapper
{
    public static CategoryResponse ToResponse(Category category) => new(
        category.Id,
        category.Name,
        category.Slug,
        category.ParentId,
        ETag.From(category.Version),
        category.CreatedAt,
        category.UpdatedAt);
}
