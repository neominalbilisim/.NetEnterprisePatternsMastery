using BuildingBlocks.Abstractions.Specifications;
using Inventory.Domain.Products;
using Inventory.Domain.Reservations;

namespace Inventory.Domain.Specifications;

/// <summary>Verilen Id listesindeki ürünler (SQL: WHERE id = ANY(...)).</summary>
public sealed class ProductsByIdsSpec(IReadOnlyCollection<Guid> productIds)
    : Specification<Product>(product => productIds.Contains(product.Id));

public sealed class ProductBySkuSpec(string sku)
    : Specification<Product>(product => product.Sku == sku);

public sealed class ReservationByOrderIdSpec(Guid orderId)
    : Specification<StockReservation>(reservation => reservation.OrderId == orderId);
