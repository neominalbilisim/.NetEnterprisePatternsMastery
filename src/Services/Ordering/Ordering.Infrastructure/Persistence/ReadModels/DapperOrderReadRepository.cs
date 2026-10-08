using BuildingBlocks.Infrastructure.Persistence;
using Dapper;
using Ordering.Application.Orders.Queries;
using Ordering.Application.Ports;

namespace Ordering.Infrastructure.Persistence.ReadModels;

/// <summary>
/// CQRS Query tarafı: Dapper ile ham SQL. Change tracking yok, yalnızca gereken kolonlar okunur.
/// Kolon eşleştirmesi Dapper.DefaultTypeMap.MatchNamesWithUnderscores ile yapılır (snake_case -> PascalCase).
/// </summary>
internal sealed class DapperOrderReadRepository(ISqlConnectionFactory connectionFactory) : IOrderReadRepository
{
    public async Task<OrderDetailsResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, customer_code, status, total_amount, rejection_reason, created_on_utc, updated_on_utc
            FROM ordering.orders
            WHERE id = @OrderId;

            SELECT product_id, product_name, unit_price, quantity, unit_price * quantity AS line_total
            FROM ordering.order_items
            WHERE order_id = @OrderId
            ORDER BY product_name;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        var order = await multi.ReadSingleOrDefaultAsync<OrderDetailsResponse>();
        if (order is null)
        {
            return null;
        }

        order.Items = (await multi.ReadAsync<OrderItemResponse>()).ToList();
        return order;
    }

    public async Task<PagedResult<OrderSummaryResponse>> GetByCustomerAsync(
        string customerCode, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*) FROM ordering.orders WHERE customer_code = @CustomerCode;

            SELECT o.id, o.status, o.total_amount, o.created_on_utc,
                   (SELECT COUNT(*) FROM ordering.order_items i WHERE i.order_id = o.id) AS item_count
            FROM ordering.orders o
            WHERE o.customer_code = @CustomerCode
            ORDER BY o.created_on_utc DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new { CustomerCode = customerCode, PageSize = pageSize, Offset = (page - 1) * pageSize },
            cancellationToken: cancellationToken));

        var totalCount = await multi.ReadSingleAsync<long>();
        var items = (await multi.ReadAsync<OrderSummaryResponse>()).ToList();

        return new PagedResult<OrderSummaryResponse>(items, page, pageSize, totalCount);
    }
}
