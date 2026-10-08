namespace Contracts.IntegrationEvents;

/// <summary>
/// RabbitMQ routing key / CAP topic isimleri. Format: {servis}.{aggregate}.{olay}
/// </summary>
public static class IntegrationEventTopics
{
    public const string OrderCreated = "ordering.order.created";
    public const string StockReserved = "inventory.stock.reserved";
    public const string StockReservationFailed = "inventory.stock.reservation-failed";
}
