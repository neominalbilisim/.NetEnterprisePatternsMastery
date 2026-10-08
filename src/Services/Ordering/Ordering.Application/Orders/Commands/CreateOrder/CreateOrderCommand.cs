using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Messaging;

namespace Ordering.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Sipariş oluşturma command'ı. IIdempotentCommand olduğu için aynı Idempotency-Key ile gelen
/// tekrar istekler ikinci bir sipariş oluşturmaz; ilk siparişin Id'si döner.
/// </summary>
public sealed record CreateOrderCommand(
    Guid IdempotencyKey,
    string CustomerCode,
    IReadOnlyList<CreateOrderItem> Items) : ICommand<Guid>, IIdempotentCommand;

public sealed record CreateOrderItem(Guid ProductId, int Quantity);
