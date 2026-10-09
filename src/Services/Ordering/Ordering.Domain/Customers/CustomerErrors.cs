using BuildingBlocks.Abstractions.Results;

namespace Ordering.Domain.Customers;

/// <summary>
/// Customer aggregate'ine ait beklenen hatalar (Result Pattern).
/// </summary>
public static class CustomerErrors
{
    public static readonly Error CodeRequired =
        Error.Validation("Customer.CodeRequired", "Customer code is required.");

    public static readonly Error NameRequired =
        Error.Validation("Customer.NameRequired", "Customer name is required.");

    public static readonly Error EmailRequired =
        Error.Validation("Customer.EmailRequired", "Customer email is required.");

    public static readonly Error InvalidEmail =
        Error.Validation("Customer.InvalidEmail", "Customer email format is invalid.");

    public static readonly Error CustomerNotActive =
        Error.Conflict("Customer.NotActive", "Customer is not active.");

    public static readonly Error CustomerAlreadyActive =
        Error.Conflict("Customer.AlreadyActive", "Customer is already active.");

    public static Error NotFound(Guid customerId) =>
        Error.NotFound("Customer.NotFound", $"Customer '{customerId}' was not found.");

    public static Error NotFoundByCode(string code) =>
        Error.NotFound("Customer.NotFoundByCode", $"Customer with code '{code}' was not found.");

    public static Error NotFoundByEmail(string email) =>
        Error.NotFound("Customer.NotFoundByEmail", $"Customer with email '{email}' was not found.");

    public static Error DuplicateCode(string code) =>
        Error.Conflict("Customer.DuplicateCode", $"A customer with code '{code}' already exists.");

    public static Error DuplicateEmail(string email) =>
        Error.Conflict("Customer.DuplicateEmail", $"A customer with email '{email}' already exists.");
}
