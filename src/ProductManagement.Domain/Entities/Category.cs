using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.Entities;

/// <summary>
/// A catalogue category. Supports a simple one-level parent reference so a tree
/// (e.g. Clothing → Jackets) can be represented without extra tables.
/// </summary>
public sealed class Category : Entity
{
    private Category() { } // EF Core

    private Category(Guid id, string name, string slug, Guid? parentId)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ParentId = parentId;
    }

    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public Guid? ParentId { get; private set; }

    public static Result<Category> Create(string name, Guid? parentId = null, string? by = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Category.NameRequired", "Category name is required.");

        var slug = SlugGenerator.Generate(name);
        if (slug.Length == 0)
            return Error.Validation("Category.InvalidName", "Category name must contain alphanumeric characters.");

        var category = new Category(Guid.CreateVersion7(), name.Trim(), slug, parentId);
        category.Stamp(by);
        return category;
    }

    public Result Rename(string name, string? by = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Category.NameRequired", "Category name is required.");

        Name = name.Trim();
        Slug = SlugGenerator.Generate(name);
        Touch(by);
        return Result.Success();
    }
}
