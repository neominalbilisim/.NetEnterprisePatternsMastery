using BuildingBlocks.Infrastructure.Persistence;
using Inventory.Domain.Products;
using Inventory.Domain.Reservations;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(InventoryDbContext context)
    : EfRepository<Product, Guid>(context), IProductRepository;

internal sealed class StockReservationRepository(InventoryDbContext context)
    : EfRepository<StockReservation, Guid>(context), IStockReservationRepository;
