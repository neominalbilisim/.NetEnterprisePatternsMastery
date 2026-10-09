using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Messaging;

namespace Ordering.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// Müşteri oluşturma command'ı. IIdempotentCommand olduğu için aynı Idempotency-Key ile gelen
/// tekrar istekler ikinci bir müşteri oluşturmaz; ilk müşterinin Id'si döner.
/// </summary>
public sealed record CreateCustomerCommand(
    Guid IdempotencyKey,
    string Code,
    string Name,
    string Email,
    string? PhoneNumber = null,
    string? TaxId = null) : ICommand<Guid>;
