namespace Inventory.Application.Ports;

/// <summary>Driven port: Inventory okuma modeli (Dapper implementasyonu Infrastructure'da).</summary>
public interface IInventoryReadRepository
{
    Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductResponse>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);

    Task<ReservationResponse?> GetReservationByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = default!;
    public string Name { get; init; } = default!;
    public decimal UnitPrice { get; init; }
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
}

public sealed class ReservationResponse
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string Status { get; init; } = default!;
    public string? RejectionReason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public List<ReservationItemResponse> Items { get; set; } = [];
}

public sealed class ReservationItemResponse
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
