using BuildingBlocks.Abstractions.IntegrationEvents;
using Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Orders.Events;

namespace Ordering.Application.Orders.EventHandlers;

/// <summary>
/// Domain Event -> Integration Event köprüsü. Domain event in-process'tir; bu handler onu
/// dış dünyaya açılan sözleşmeye (OrderCreatedIntegrationEvent) çevirip Outbox'a yazar.
/// UnitOfWork.SaveChangesAsync içinde, açık transaction varken çalışır.
/// </summary>
internal sealed class OrderCreatedDomainEventHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<OrderCreatedDomainEvent>
{
    public Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderCreatedIntegrationEvent(
            notification.OrderId,
            notification.CustomerCode,
            notification.Lines.Select(l => new OrderCreatedLine(l.ProductId, l.Quantity)).ToList());

        return publisher.PublishAsync(IntegrationEventTopics.OrderCreated, integrationEvent, cancellationToken);
    }
}

/// <summary>
/// Aynı domain event'e birden fazla handler bağlanabilir. Burada örnek olarak yalnızca loglama yapılır;
/// gerçek senaryoda müşteri bildirimi, e-posta vb. tetiklenebilir.
/// </summary>
internal sealed class OrderConfirmedDomainEventHandler(ILogger<OrderConfirmedDomainEventHandler> logger)
    : INotificationHandler<OrderConfirmedDomainEvent>
{
    public Task Handle(OrderConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} confirmed. Customer notification could be sent here.", notification.OrderId);
        return Task.CompletedTask;
    }
}

internal sealed class OrderRejectedDomainEventHandler(ILogger<OrderRejectedDomainEventHandler> logger)
    : INotificationHandler<OrderRejectedDomainEvent>
{
    public Task Handle(OrderRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning("Order {OrderId} rejected: {Reason}", notification.OrderId, notification.Reason);
        return Task.CompletedTask;
    }
}
