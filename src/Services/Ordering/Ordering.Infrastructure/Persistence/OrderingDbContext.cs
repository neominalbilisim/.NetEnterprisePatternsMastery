using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Customers;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence;

/// <summary>Command tarafının EF Core DbContext'i. Tablolar "ordering" şemasında, snake_case isimlendirme ile.</summary>
public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public const string Schema = "ordering";

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
        modelBuilder.ApplyBuildingBlocksConfigurations();
    }
}
