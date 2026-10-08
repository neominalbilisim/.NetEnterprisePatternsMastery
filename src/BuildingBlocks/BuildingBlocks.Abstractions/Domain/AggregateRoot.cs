namespace BuildingBlocks.Abstractions.Domain;

/// <summary>
/// Aggregate'in dış dünyaya açılan tek kapısı. Tutarlılık kuralları aggregate root üzerinden korunur;
/// repository'ler yalnızca aggregate root'lar için tanımlanır.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Domain event'i biriktirir; kayıt anında (SaveChanges) dispatch edilir.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>Aggregate root işaretleyici arayüzü.</summary>
public interface IAggregateRoot
{
}
