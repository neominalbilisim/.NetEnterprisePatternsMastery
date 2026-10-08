using Ordering.Application.Orders.Queries;

namespace Ordering.Application.Ports;

/// <summary>
/// Driven port: CQRS okuma tarafı. Implementasyonu Dapper ile yazılır; domain modeli yerine
/// doğrudan ekranın ihtiyaç duyduğu DTO'lar (read model) döner.
/// </summary>
public interface IOrderReadRepository
{
    Task<OrderDetailsResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryResponse>> GetByCustomerAsync(
        string customerCode, int page, int pageSize, CancellationToken cancellationToken = default);
}
