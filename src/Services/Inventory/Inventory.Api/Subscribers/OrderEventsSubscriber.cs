using Contracts.IntegrationEvents;
using DotNetCore.CAP;
using Inventory.Application.Reservations.Commands;
using MediatR;

namespace Inventory.Api.Subscribers;

/// <summary>
/// OrderCreated integration event'ini dinler ve stok rezervasyonu command'ına çevirir.
/// Event'in EventId'si inbox anahtarıdır; CAP aynı mesajı tekrar teslim etse bile rezervasyon bir kez yapılır.
/// </summary>
public sealed class OrderEventsSubscriber(ISender sender, ILogger<OrderEventsSubscriber> logger) : ICapSubscribe
{
    [CapSubscribe(IntegrationEventTopics.OrderCreated)]
    public async Task HandleOrderCreatedAsync(OrderCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received OrderCreated for order {OrderId} ({EventId})",
            integrationEvent.OrderId, integrationEvent.EventId);

        var command = new ReserveStockCommand(
            integrationEvent.EventId,
            integrationEvent.OrderId,
            integrationEvent.Lines.Select(l => new ReserveStockItem(l.ProductId, l.Quantity)).ToList());

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Stock reservation for order {OrderId} failed: {ErrorCode}",
                integrationEvent.OrderId, result.Error.Code);
        }
    }
}
