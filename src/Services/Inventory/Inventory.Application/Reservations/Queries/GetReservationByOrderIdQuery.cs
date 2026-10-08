using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Results;
using Inventory.Application.Ports;

namespace Inventory.Application.Reservations.Queries;

public sealed record GetReservationByOrderIdQuery(Guid OrderId) : IQuery<ReservationResponse>;

internal sealed class GetReservationByOrderIdQueryHandler(IInventoryReadRepository readRepository)
    : IQueryHandler<GetReservationByOrderIdQuery, ReservationResponse>
{
    public async Task<Result<ReservationResponse>> Handle(GetReservationByOrderIdQuery query, CancellationToken cancellationToken)
    {
        var reservation = await readRepository.GetReservationByOrderIdAsync(query.OrderId, cancellationToken);
        if (reservation is null)
        {
            return Error.NotFound("Reservation.NotFound", $"No reservation found for order '{query.OrderId}'.");
        }

        return reservation;
    }
}
