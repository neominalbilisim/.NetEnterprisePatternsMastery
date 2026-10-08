namespace BuildingBlocks.Abstractions.Domain;

/// <summary>
/// Domain event biriktirebilen nesneler. UnitOfWork, kayıt sırasında bu event'leri toplayıp dispatch eder.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
