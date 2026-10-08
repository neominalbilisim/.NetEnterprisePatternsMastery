using BuildingBlocks.Abstractions.Specifications;

namespace Ordering.Domain.Orders.Specifications;

/// <summary>Siparişi kalemleriyle birlikte getirir (Include).</summary>
public sealed class OrderByIdWithItemsSpec : Specification<Order>
{
    public OrderByIdWithItemsSpec(Guid orderId) : base(order => order.Id == orderId)
    {
        AddInclude(order => order.Items);
    }
}

/// <summary>Belirli bir müşterinin siparişleri.</summary>
public sealed class OrdersByCustomerSpec(string customerCode)
    : Specification<Order>(order => order.CustomerCode == customerCode);

/// <summary>Belirli bir durumdaki siparişler.</summary>
public sealed class OrdersByStatusSpec(OrderStatus status)
    : Specification<Order>(order => order.Status == status);

// Kullanım örneği (birleştirme):
// new OrdersByCustomerSpec("C-1001").And(new OrdersByStatusSpec(OrderStatus.Pending))
