using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.Infrastructure.Persistence.Seed;

/// <summary>Idempotent seed data: a small fashion catalogue for demos/tests.</summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct))
            return; // already seeded

        var clothing = Category.Create("Clothing", by: "seed").Value;
        var jackets = Category.Create("Jackets", parentId: clothing.Id, by: "seed").Value;
        var tshirts = Category.Create("T-Shirts", parentId: clothing.Id, by: "seed").Value;
        db.Categories.AddRange(clothing, jackets, tshirts);

        var jacket = Product.Create(
            "Classic Denim Jacket",
            jackets.Id,
            description: "A timeless denim jacket that pairs with everything.",
            brand: "Northwind",
            attributes: new Dictionary<string, object?> { ["material"] = "Denim", ["season"] = "All-season" },
            by: "seed").Value;
        jacket.AddVariant("DENIM-JKT-S-BLUE", Money.Create(79.90m, "USD").Value, 25, new Dictionary<string, object?> { ["size"] = "S", ["color"] = "Blue" }, "seed");
        jacket.AddVariant("DENIM-JKT-M-BLUE", Money.Create(79.90m, "USD").Value, 40, new Dictionary<string, object?> { ["size"] = "M", ["color"] = "Blue" }, "seed");
        jacket.AddVariant("DENIM-JKT-L-BLUE", Money.Create(79.90m, "USD").Value, 15, new Dictionary<string, object?> { ["size"] = "L", ["color"] = "Blue" }, "seed");
        jacket.ChangeStatus(ProductStatus.Active, "seed");

        var tee = Product.Create(
            "Organic Cotton Tee",
            tshirts.Id,
            description: "Soft, breathable everyday t-shirt.",
            brand: "Northwind",
            attributes: new Dictionary<string, object?> { ["material"] = "Organic Cotton", ["fit"] = "Regular" },
            by: "seed").Value;
        tee.AddVariant("TEE-M-WHITE", Money.Create(19.50m, "USD").Value, 100, new Dictionary<string, object?> { ["size"] = "M", ["color"] = "White" }, "seed");
        tee.AddVariant("TEE-M-BLACK", Money.Create(19.50m, "USD").Value, 80, new Dictionary<string, object?> { ["size"] = "M", ["color"] = "Black" }, "seed");
        tee.ChangeStatus(ProductStatus.Active, "seed");

        db.Products.AddRange(jacket, tee);
        await db.SaveChangesAsync(ct);
    }
}
