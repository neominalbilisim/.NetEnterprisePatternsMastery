using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Customers;

namespace Ordering.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever(); // Id domain'de üretilir

        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique(); // Code benzersiz olmalı

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();

        builder.Property(c => c.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique(); // Email benzersiz olmalı

        builder.Property(c => c.PhoneNumber).HasMaxLength(20);

        builder.Property(c => c.TaxId).HasMaxLength(50);

        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(c => c.CreatedOnUtc).IsRequired();
        builder.Property(c => c.UpdatedOnUtc);

        // Composite index: aktif müşterileri sorgulamak için
        builder.HasIndex(c => new { c.IsActive, c.Code });
    }
}
