using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Results;
using Ordering.Application.Ports;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailsResponse>;

/// <summary>Query tarafı: EF Core / aggregate kullanılmaz; Dapper read repository'si doğrudan DTO döner.</summary>
internal sealed class GetOrderByIdQueryHandler(IOrderReadRepository readRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderDetailsResponse>
{
    public async Task<Result<OrderDetailsResponse>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await readRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(query.OrderId);
        }

        return order;
    }
}
