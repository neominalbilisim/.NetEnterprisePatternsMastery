using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using Inventory.Domain.Products;
using Inventory.Domain.Reservations;
using Inventory.Domain.Specifications;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Reservations.Commands;

/// <summary>
/// OrderCreated integration event'i ile tetiklenir. IInboxCommand: aynı event tekrar gelirse işlenmez.
/// </summary>
public sealed record ReserveStockCommand(Guid MessageId, Guid OrderId, IReadOnlyList<ReserveStockItem> Items)
    : ICommand, IInboxCommand
{
    public string ConsumerName => "inventory.reserve-stock";
}

public sealed record ReserveStockItem(Guid ProductId, int Quantity);

internal sealed class ReserveStockCommandHandler(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReserveStockCommandHandler> logger) : ICommandHandler<ReserveStockCommand>
{
    public async Task<Result> Handle(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        // İkinci savunma hattı: Inbox'a ek olarak iş kuralı seviyesinde idempotency.
        if (await reservationRepository.AnyAsync(new ReservationByOrderIdSpec(command.OrderId), cancellationToken))
        {
            logger.LogInformation("Reservation for order {OrderId} already exists; skipping", command.OrderId);
            return Result.Success();
        }

        var productIds = command.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = (await productRepository.ListAsync(new ProductsByIdsSpec(productIds), cancellationToken))
            .ToDictionary(p => p.Id);

        // Önce tüm kalemler kontrol edilir: ya hepsi rezerve edilir ya hiçbiri (all-or-nothing).
        var problems = new List<string>();
        foreach (var item in command.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                problems.Add($"Product '{item.ProductId}' not found");
            }
            else if (!product.CanReserve(item.Quantity))
            {
                problems.Add($"Insufficient stock for '{product.Sku}' (requested {item.Quantity}, available {product.AvailableQuantity})");
            }
        }

        StockReservation reservation;
        if (problems.Count > 0)
        {
            // Başarısızlık da bir iş sonucudur: kaydedilir ve domain event ile Ordering'e bildirilir.
            reservation = StockReservation.Reject(command.OrderId, string.Join("; ", problems));
            logger.LogWarning("Stock reservation rejected for order {OrderId}: {Reason}", command.OrderId, reservation.RejectionReason);
        }
        else
        {
            foreach (var item in command.Items)
            {
                var reserveResult = products[item.ProductId].Reserve(item.Quantity);
                if (reserveResult.IsFailure)
                {
                    return reserveResult;
                }
            }

            reservation = StockReservation.Reserve(
                command.OrderId,
                command.Items.Select(i => new ReservationLine(i.ProductId, i.Quantity)).ToList());
            logger.LogInformation("Stock reserved for order {OrderId}", command.OrderId);
        }

        reservationRepository.Add(reservation);

        // Eşzamanlı rezervasyonlarda xmin çakışması -> DbUpdateConcurrencyException -> transaction geri alınır
        // -> CAP mesajı yeniden dener; ikinci denemede güncel stok ile karar verilir.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
