using BuildingBlocks.Infrastructure.Persistence;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(OrderingDbContext context)
    : EfRepository<Order, Guid>(context), IOrderRepository;
