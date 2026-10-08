namespace Ordering.Application.Orders.Queries;

// Read model DTO'ları: Dapper doğrudan bu sınıflara map eder (domain nesnesi değildir).

public sealed class OrderDetailsResponse
{
    public Guid Id { get; init; }
    public string CustomerCode { get; init; } = default!;
    public string Status { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public List<OrderItemResponse> Items { get; set; } = [];
}

public sealed class OrderItemResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class OrderSummaryResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public long ItemCount { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);
