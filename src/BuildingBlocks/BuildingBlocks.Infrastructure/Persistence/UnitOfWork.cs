using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Abstractions.Persistence;
using DotNetCore.CAP;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Unit of Work adapter'ı: EF Core DbContext + CAP transaction.
///
/// Akış:
/// 1) TransactionBehavior -> BeginTransaction(): DB transaction açılır ve ICapPublisher bu transaction'a bağlanır.
/// 2) Handler -> SaveChangesAsync(): Değişiklikler yazılır, biriken domain event'ler dispatch edilir.
/// 3) Domain event handler -> IIntegrationEventPublisher: CAP mesajı cap.published tablosuna AYNI transaction ile yazar (Outbox).
/// 4) TransactionBehavior -> Commit: İş verisi + outbox atomik olarak commit edilir, ardından CAP mesajı broker'a iletir.
/// </summary>
public sealed class UnitOfWork<TContext>(
    TContext context,
    ICapPublisher capPublisher,
    IPublisher publisher,
    ILogger<UnitOfWork<TContext>> logger) : IUnitOfWork
    where TContext : DbContext
{
    public bool HasActiveTransaction => context.Database.CurrentTransaction is not null;

    public ITransactionScope BeginTransaction()
    {
        // ÖNEMLİ: Bu metot bilinçli olarak senkrondur. CAP, aktif transaction'ı AsyncLocal ile taşır.
        // AsyncLocal değeri async bir metot içinde set edilseydi, metot dönünce çağırana geri akmazdı.
        // Senkron çağrıda ise TransactionBehavior'ın devamındaki tüm akış aynı transaction'ı görür.
        var transaction = context.Database.BeginTransaction(capPublisher, autoCommit: false);
        return new EfCapTransactionScope(transaction);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Domain event'leri kaydetmeden önce topla ve temizle (tekrar dispatch edilmesinler).
        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        var affectedRows = await context.SaveChangesAsync(cancellationToken);

        // Kayıt başarılı olduktan sonra, hâlâ aynı transaction içindeyken dispatch edilir.
        // Handler'lardan biri hata verirse transaction commit edilmez; veri ve event tutarlı kalır.
        foreach (var domainEvent in domainEvents)
        {
            logger.LogDebug("Dispatching domain event {DomainEventType} ({EventId})",
                domainEvent.GetType().Name, domainEvent.EventId);
            await publisher.Publish(domainEvent, cancellationToken);
        }

        return affectedRows;
    }

    private sealed class EfCapTransactionScope(IDbContextTransaction transaction) : ITransactionScope
    {
        // CAP'in sarmaladığı transaction, commit sonrası outbox mesajlarını dispatcher'a iletir.
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
