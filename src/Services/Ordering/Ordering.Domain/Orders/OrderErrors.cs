using BuildingBlocks.Abstractions.Results;

namespace Ordering.Domain.Orders;

/// <summary>Order aggregate'ine ait beklenen hatalar (Result Pattern).</summary>
public static class OrderErrors
{
    public static readonly Error CustomerRequired =
        Error.Validation("Order.CustomerRequired", "Customer code is required.");

    public static readonly Error EmptyOrder =
        Error.Validation("Order.Empty", "An order must contain at least one item.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Order.InvalidQuantity", "Item quantity must be greater than zero.");

    public static Error NotFound(Guid orderId) =>
        Error.NotFound("Order.NotFound", $"Order '{orderId}' was not found.");

    public static Error InvalidStatusTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("Order.InvalidStatusTransition", $"Order status cannot change from '{from}' to '{to}'.");

    public static Error TooManyPendingOrders(int limit) =>
        Error.Conflict("Order.TooManyPendingOrders", $"Customer already has {limit} pending orders.");

    public static Error CreditLimitExceeded(decimal total, decimal availableCredit) =>
        Error.Failure("Order.CreditLimitExceeded",
            $"Order total {total:N2} exceeds the available credit {availableCredit:N2}.");

    public static Error ProductsNotFound(IEnumerable<Guid> productIds) =>
        Error.NotFound("Order.ProductsNotFound", $"Products not found: {string.Join(", ", productIds)}.");

    public static Error InsufficientStock(IEnumerable<string> productNames) =>
        Error.Conflict("Order.InsufficientStock", $"Insufficient stock for: {string.Join(", ", productNames)}.");
}
