using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Infrastructure.Persistence.Idempotency;
using BuildingBlocks.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Her servisin DbContext'ine ortak tabloları (inbox, idempotency) ekler ve
    /// aggregate'lerdeki DomainEvents koleksiyonunu EF Core modelinden hariç tutar.
    /// ApplyConfigurationsFromAssembly çağrısından SONRA çağrılmalıdır.
    /// </summary>
    public static ModelBuilder ApplyBuildingBlocksConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(m => new { m.MessageId, m.Consumer });
            builder.Property(m => m.Consumer).HasMaxLength(200);
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.ToTable("idempotency_records");
            builder.HasKey(r => new { r.Key, r.RequestName });
            builder.Property(r => r.RequestName).HasMaxLength(200);
            builder.Property(r => r.ResponseJson).HasColumnType("jsonb");
        });

        var aggregateTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(IHasDomainEvents).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var type in aggregateTypes)
        {
            modelBuilder.Entity(type).Ignore(nameof(IHasDomainEvents.DomainEvents));
        }

        return modelBuilder;
    }
}
