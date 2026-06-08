using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Persistence.Converters;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // app-assigned Guid v7

        builder.Property(p => p.Name).IsRequired().HasMaxLength(250);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(280);
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Brand).HasMaxLength(200);

        builder.Property(p => p.Status)
            .HasConversion<string>()   // store enum as readable text
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.Property(p => p.IsDeleted).HasDefaultValue(false);

        // Schema-less attributes -> jsonb (mapped via private field).
        builder.Ignore(p => p.Attributes);
        builder.Property<Dictionary<string, object?>>("_attributes")
            .HasColumnName("attributes")
            .HasColumnType("jsonb")
            .HasConversion(new JsonAttributesConverter(), new JsonAttributesComparer())
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

        // Variants owned via the product aggregate (write through the field).
        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Product.Variants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique slug among non-deleted products.
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.IsDeleted);

        // GIN index for efficient containment queries on the JSONB attributes.
        builder.HasIndex("_attributes").HasMethod("gin");

        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
