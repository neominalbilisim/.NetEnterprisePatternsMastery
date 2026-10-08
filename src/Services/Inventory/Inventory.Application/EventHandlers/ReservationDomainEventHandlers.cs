using BuildingBlocks.Abstractions.IntegrationEvents;
using Contracts.IntegrationEvents;
using Inventory.Domain.Reservations;
using MediatR;

namespace Inventory.Application.EventHandlers;

/// <summary>Domain event -> Integration event (Outbox, aynı transaction).</summary>
internal sealed class StockReservedDomainEventHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<StockReservedDomainEvent>
{
    public Task Handle(StockReservedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            IntegrationEventTopics.StockReserved,
            new StockReservedIntegrationEvent(notification.OrderId, notification.ReservationId),
            cancellationToken);
}

internal sealed class StockReservationRejectedDomainEventHandler(IIntegrationEventPublisher publisher)
    : INotificationHandler<StockReservationRejectedDomainEvent>
{
    public Task Handle(StockReservationRejectedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            IntegrationEventTopics.StockReservationFailed,
            new StockReservationFailedIntegrationEvent(notification.OrderId, notification.ReservationId, notification.Reason),
            cancellationToken);
}
