using BuildingBlocks.Abstractions.IntegrationEvents;

namespace Contracts.IntegrationEvents;

/// <summary>
/// Sipariş oluşturulduğunda Ordering servisi tarafından yayınlanır; Inventory stok rezervasyonu yapar.
/// Not: Domain event'in (OrderCreatedDomainEvent) birebir kopyası değildir; dış dünyaya yalnızca gereken bilgi açılır.
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    string CustomerCode,
    IReadOnlyList<OrderCreatedLine> Lines) : IntegrationEvent;

public sealed record OrderCreatedLine(Guid ProductId, int Quantity);
