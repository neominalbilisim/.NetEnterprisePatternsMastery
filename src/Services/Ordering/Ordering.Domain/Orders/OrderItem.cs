using BuildingBlocks.Abstractions.Domain;

namespace Ordering.Domain.Orders;

/// <summary>
/// Order aggregate'ine ait entity. Dışarıdan doğrudan değiştirilemez; yalnızca Order üzerinden yönetilir.
/// </summary>
public sealed class OrderItem : Entity<Guid>
{
    private OrderItem()
    {
    }

    internal OrderItem(Guid id, Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = default!;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
