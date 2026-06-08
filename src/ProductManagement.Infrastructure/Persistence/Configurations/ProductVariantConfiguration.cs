using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Persistence.Converters;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever(); // app-assigned Guid v7

        builder.Property(v => v.Sku).IsRequired().HasMaxLength(100);
        builder.HasIndex(v => v.Sku).IsUnique();

        builder.Property(v => v.StockQuantity).IsRequired();
        builder.Property(v => v.ReservedQuantity).IsRequired();

        // Money as an owned value object (two columns, currency never lost).
        builder.OwnsOne(v => v.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_amount").HasColumnType("numeric(18,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(v => v.Price).IsRequired();

        builder.Ignore(v => v.Attributes);
        builder.Property<Dictionary<string, object?>>("_attributes")
            .HasColumnName("attributes")
            .HasColumnType("jsonb")
            .HasConversion(new JsonAttributesConverter(), new JsonAttributesComparer())
            .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(v => v.CreatedBy).HasMaxLength(100);
        builder.Property(v => v.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(v => v.ProductId);

        builder.Property(v => v.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // DB-level guard: availability can never go negative even via raw SQL.
        builder.ToTable(t => t.HasCheckConstraint("ck_variant_stock_non_negative", "stock_quantity >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_variant_reserved_within_stock", "reserved_quantity >= 0 AND reserved_quantity <= stock_quantity"));
    }
}
