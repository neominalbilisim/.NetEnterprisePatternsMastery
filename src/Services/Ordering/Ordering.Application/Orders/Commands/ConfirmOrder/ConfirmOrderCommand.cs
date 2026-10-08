using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.ConfirmOrder;

/// <summary>
/// StockReserved integration event'i sonucu tetiklenir. IInboxCommand olduğu için
/// aynı event ikinci kez gelirse handler çalıştırılmaz (idempotent consumer).
/// </summary>
public sealed record ConfirmOrderCommand(Guid OrderId, Guid MessageId) : ICommand, IInboxCommand
{
    public string ConsumerName => "ordering.confirm-order";
}

internal sealed class ConfirmOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmOrderCommand>
{
    public async Task<Result> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(command.OrderId);
        }

        var result = order.Confirm();
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
