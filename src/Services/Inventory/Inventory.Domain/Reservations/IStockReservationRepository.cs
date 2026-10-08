using BuildingBlocks.Abstractions.Persistence;

namespace Inventory.Domain.Reservations;

public interface IStockReservationRepository : IRepository<StockReservation, Guid>
{
}
