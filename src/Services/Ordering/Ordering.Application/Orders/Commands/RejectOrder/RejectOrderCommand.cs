using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Commands.RejectOrder;

/// <summary>
/// StockReservationFailed integration event'i sonucu tetiklenir (compensating action).
/// </summary>
public sealed record RejectOrderCommand(Guid OrderId, string Reason, Guid MessageId) : ICommand, IInboxCommand
{
    public string ConsumerName => "ordering.reject-order";
}

internal sealed class RejectOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RejectOrderCommand>
{
    public async Task<Result> Handle(RejectOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound(command.OrderId);
        }

        var result = order.Reject(command.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
