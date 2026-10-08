using BuildingBlocks.Abstractions.Results;

namespace Ordering.Application.Ports;

/// <summary>
/// Driven port: Stok ve fiyat bilgisi. Implementasyonu gRPC adapter'ıdır (senkron iletişim).
/// </summary>
public interface IInventoryService
{
    Task<Result<IReadOnlyList<ProductAvailability>>> CheckAvailabilityAsync(
        IReadOnlyCollection<ProductQuantity> items, CancellationToken cancellationToken = default);
}

public sealed record ProductQuantity(Guid ProductId, int Quantity);

public sealed record ProductAvailability(
    Guid ProductId,
    bool Exists,
    string ProductName,
    decimal UnitPrice,
    int RequestedQuantity,
    int AvailableQuantity,
    bool IsAvailable);
