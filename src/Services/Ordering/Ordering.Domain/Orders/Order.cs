using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Abstractions.Results;
using Ordering.Domain.Orders.Events;

namespace Ordering.Domain.Orders;

/// <summary>
/// Order aggregate root. Durum geçişleri ve tutar hesaplama kuralları burada korunur (rich domain model).
/// Setter'lar private'tır; state yalnızca anlamlı domain metotlarıyla değişir.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
        // EF Core için
    }

    private Order(Guid id, string customerCode) : base(id)
    {
        CustomerCode = customerCode;
        Status = OrderStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string CustomerCode { get; private set; } = default!;

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>Factory metot: Geçersiz bir Order nesnesinin oluşmasını engeller.</summary>
    public static Result<Order> Create(string customerCode, IReadOnlyCollection<OrderLine> lines)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
        {
            return OrderErrors.CustomerRequired;
        }

        if (lines.Count == 0)
        {
            return OrderErrors.EmptyOrder;
        }

        if (lines.Any(line => line.Quantity <= 0))
        {
            return OrderErrors.InvalidQuantity;
        }

        var order = new Order(Guid.NewGuid(), customerCode.Trim());

        foreach (var line in lines)
        {
            order._items.Add(new OrderItem(
                Guid.NewGuid(), order.Id, line.ProductId, line.ProductName, line.UnitPrice, line.Quantity));
        }

        order.TotalAmount = order._items.Sum(item => item.LineTotal);

        order.Raise(new OrderCreatedDomainEvent(order.Id, order.CustomerCode, order.TotalAmount, lines.ToList()));

        return order;
    }

    /// <summary>Stok rezerve edildiğinde çağrılır. Yalnızca Pending durumundan geçiş yapılabilir.</summary>
    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.InvalidStatusTransition(Status, OrderStatus.Confirmed);
        }

        Status = OrderStatus.Confirmed;
        UpdatedOnUtc = DateTime.UtcNow;
        Raise(new OrderConfirmedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>Stok rezerve edilemediğinde çağrılır (compensation).</summary>
    public Result Reject(string reason)
    {
        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.InvalidStatusTransition(Status, OrderStatus.Rejected);
        }

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        UpdatedOnUtc = DateTime.UtcNow;
        Raise(new OrderRejectedDomainEvent(Id, reason));

        return Result.Success();
    }
}
