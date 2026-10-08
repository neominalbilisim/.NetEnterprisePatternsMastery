using Inventory.Domain.Products;
using Inventory.Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.UnitPrice).HasPrecision(18, 2);

        // Npgsql: uint + IsRowVersion => PostgreSQL xmin sistem kolonu (optimistic concurrency)
        builder.Property(p => p.Version).IsRowVersion();
    }
}

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);

        // Aynı sipariş için tek rezervasyon (idempotency'nin veritabanı seviyesindeki garantisi)
        builder.HasIndex(r => r.OrderId).IsUnique();

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StockReservationItemConfiguration : IEntityTypeConfiguration<StockReservationItem>
{
    public void Configure(EntityTypeBuilder<StockReservationItem> builder)
    {
        builder.ToTable("stock_reservation_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
    }
}
