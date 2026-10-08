namespace BuildingBlocks.Abstractions.IntegrationEvents;

/// <summary>
/// Servis sınırını aşan, message broker üzerinden asenkron iletilen olay.
/// Domain event'ten farklı olarak dış dünyaya açılan bir SÖZLEŞMEDİR; bu yüzden sade ve sürümlenebilir tutulur.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Integration event yayınlama portu. Implementasyon (CAP) Outbox Pattern'i uygular:
/// mesaj önce iş verisiyle aynı transaction'da veritabanına yazılır, commit sonrası broker'a iletilir.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(string topic, TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
