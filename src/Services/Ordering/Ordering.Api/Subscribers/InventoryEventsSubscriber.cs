using Contracts.IntegrationEvents;
using DotNetCore.CAP;
using MediatR;
using Ordering.Application.Orders.Commands.ConfirmOrder;
using Ordering.Application.Orders.Commands.RejectOrder;

namespace Ordering.Api.Subscribers;

/// <summary>
/// Driving adapter (messaging): Inventory servisinden gelen integration event'leri dinler ve command'a çevirir.
/// - MessageId olarak event'in EventId'si kullanılır -> InboxBehavior ile idempotent tüketim.
/// - Exception fırlatılırsa CAP mesajı FailedRetryCount kadar yeniden dener (resiliency).
/// - İş hatası (Result.Failure) loglanır; mesaj tekrar denenmez, çünkü tekrar denemek sonucu değiştirmez.
/// </summary>
public sealed class InventoryEventsSubscriber(ISender sender, ILogger<InventoryEventsSubscriber> logger) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventTopics.StockReserved)]
    public async Task HandleStockReservedAsync(StockReservedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ConfirmOrderCommand(integrationEvent.OrderId, integrationEvent.EventId), cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Could not confirm order {OrderId}: {ErrorCode}", integrationEvent.OrderId, result.Error.Code);
        }
    }

    [CapSubscribe(IntegrationEventTopics.StockReservationFailed)]
    public async Task HandleStockReservationFailedAsync(
        StockReservationFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RejectOrderCommand(integrationEvent.OrderId, integrationEvent.Reason, integrationEvent.EventId),
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Could not reject order {OrderId}: {ErrorCode}", integrationEvent.OrderId, result.Error.Code);
        }
    }
}
