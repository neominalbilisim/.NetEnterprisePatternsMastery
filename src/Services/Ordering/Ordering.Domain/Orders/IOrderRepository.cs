using BuildingBlocks.Abstractions.Persistence;

namespace Ordering.Domain.Orders;

/// <summary>Order aggregate'i için repository portu (driven port). Implementasyonu Infrastructure'dadır.</summary>
public interface IOrderRepository : IRepository<Order, Guid>
{
}
