using BuildingBlocks.Infrastructure.Persistence;
using Dapper;
using Inventory.Application.Ports;

namespace Inventory.Infrastructure.Persistence.ReadModels;

/// <summary>CQRS okuma tarafı: Dapper + ham SQL.</summary>
internal sealed class DapperInventoryReadRepository(ISqlConnectionFactory connectionFactory) : IInventoryReadRepository
{
  private const string ProductColumns = "id, sku, name, unit_price, available_quantity, reserved_quantity";

  public async Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
  {
    await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
    return await connection.QuerySingleOrDefaultAsync<ProductResponse>(new CommandDefinition(
        $"SELECT {ProductColumns} FROM inventory.products WHERE id = @ProductId",
        new { ProductId = productId },
        cancellationToken: cancellationToken));
  }

  public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
  {
    await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
    var products = await connection.QueryAsync<ProductResponse>(new CommandDefinition(
        $"SELECT {ProductColumns} FROM inventory.products ORDER BY sku",
        cancellationToken: cancellationToken));
    return products.ToList();
  }

  public async Task<IReadOnlyList<ProductResponse>> GetProductsByIdsAsync(
      IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
  {
    await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

    // PostgreSQL dizisi olarak tek parametre gönderilir: WHERE id = ANY(@Ids)
    // WHERE id IN (...) yerine WHERE id = ANY(@Ids) kullanmak Npgsql tarafında query plan caching (prepared statements) performansını ciddi oranda artırır.
    var products = await connection.QueryAsync<ProductResponse>(new CommandDefinition(
            $"SELECT {ProductColumns} FROM inventory.products WHERE id = ANY(@Ids)",
            new { Ids = productIds.ToArray() },
            cancellationToken: cancellationToken));
    return products.ToList();
  }

  public async Task<ReservationResponse?> GetReservationByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    const string sql = """
            SELECT id, order_id, status, rejection_reason, created_on_utc
            FROM inventory.stock_reservations
            WHERE order_id = @OrderId;

            SELECT i.product_id, i.quantity
            FROM inventory.stock_reservation_items i
            JOIN inventory.stock_reservations r ON r.id = i.reservation_id
            WHERE r.order_id = @OrderId;
            """;

    await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);


    // QueryMultipleAsync İle N+1 Çözümü: Tek veritabanı round-trip'i ile iki tabloyu birden çekmeniz veritabanı trafiğini yarıya indirir.
    using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));

    var reservation = await multi.ReadSingleOrDefaultAsync<ReservationResponse>();
    if (reservation is null)
    {
      return null;
    }

    reservation.Items = (await multi.ReadAsync<ReservationItemResponse>()).ToList();
    return reservation;
  }
}
