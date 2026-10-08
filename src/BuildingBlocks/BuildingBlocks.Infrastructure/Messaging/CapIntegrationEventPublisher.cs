using BuildingBlocks.Abstractions.IntegrationEvents;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// IIntegrationEventPublisher portunun CAP adapter'ı (Outbox Pattern).
/// Aktif bir CAP transaction'ı varken PublishAsync mesajı broker'a DEĞİL, cap.published tablosuna yazar.
/// Transaction commit edildiğinde CAP mesajı RabbitMQ'ya iletir; iletim başarısız olursa arka planda yeniden dener.
/// </summary>
internal sealed class CapIntegrationEventPublisher(
    ICapPublisher capPublisher,
    ILogger<CapIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public async Task PublishAsync<TEvent>(string topic, TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var headers = new Dictionary<string, string?>
        {
            ["event-id"] = integrationEvent.EventId.ToString(),
            ["event-type"] = typeof(TEvent).Name
        };

        await capPublisher.PublishAsync(topic, integrationEvent, headers, cancellationToken);

        logger.LogInformation(
            "Integration event {EventType} ({EventId}) stored in outbox for topic {Topic}",
            typeof(TEvent).Name, integrationEvent.EventId, topic);
    }
}
