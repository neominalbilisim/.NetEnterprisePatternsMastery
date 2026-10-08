namespace BuildingBlocks.Abstractions.Domain;

/// <summary>
/// Domain event'ler için temel record. Olay adları geçmiş zaman ile isimlendirilir (örn. OrderCreated).
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
