using BuildingBlocks.Abstractions.IntegrationEvents;

namespace Contracts.IntegrationEvents;

/// <summary>Stok başarıyla rezerve edildiğinde Inventory servisi tarafından yayınlanır.</summary>
public sealed record StockReservedIntegrationEvent(Guid OrderId, Guid ReservationId) : IntegrationEvent;

/// <summary>Stok rezerve edilemediğinde (yetersiz stok / bilinmeyen ürün) yayınlanır.</summary>
public sealed record StockReservationFailedIntegrationEvent(Guid OrderId, Guid ReservationId, string Reason) : IntegrationEvent;
