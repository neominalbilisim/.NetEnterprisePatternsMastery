using BuildingBlocks.Abstractions.Domain;

namespace Ordering.Domain.Orders.Events;

public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string CustomerCode,
    decimal TotalAmount,
    IReadOnlyList<OrderLine> Lines) : DomainEvent;

public sealed record OrderConfirmedDomainEvent(Guid OrderId) : DomainEvent;

public sealed record OrderRejectedDomainEvent(Guid OrderId, string Reason) : DomainEvent;
