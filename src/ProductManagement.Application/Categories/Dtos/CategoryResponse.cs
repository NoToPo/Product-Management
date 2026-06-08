namespace ProductManagement.Application.Categories.Dtos;

/// <summary>Read-model for a category.</summary>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    string ETag,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
