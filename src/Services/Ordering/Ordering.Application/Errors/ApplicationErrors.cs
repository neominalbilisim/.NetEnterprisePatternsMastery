using BuildingBlocks.Abstractions.Results;

namespace Ordering.Application.Errors;

public static class CustomerErrors
{
    public static Error NotFound(string customerCode) =>
        Error.NotFound("Customer.NotFound", $"Customer '{customerCode}' was not found.");

    public static Error Inactive(string customerCode) =>
        Error.Failure("Customer.Inactive", $"Customer '{customerCode}' is not active and cannot place orders.");

    public static readonly Error ServiceUnavailable = Error.Unavailable(
        "Customer.ServiceUnavailable",
        "Customer credit service is temporarily unavailable. Please try again later.");
}

public static class InventoryErrors
{
    public static readonly Error ServiceUnavailable = Error.Unavailable(
        "Inventory.ServiceUnavailable",
        "Inventory service is temporarily unavailable. Please try again later.");
}
