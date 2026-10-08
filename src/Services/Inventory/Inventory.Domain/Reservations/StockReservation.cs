using BuildingBlocks.Abstractions.Domain;

namespace Inventory.Domain.Reservations;

public enum ReservationStatus
{
    Reserved = 1,
    Rejected = 2
}

public sealed record ReservationLine(Guid ProductId, int Quantity);

/// <summary>
/// Bir siparişe ait stok rezervasyonu (başarılı ya da reddedilmiş). OrderId üzerinde UNIQUE index vardır:
/// inbox kaydı herhangi bir sebeple atlansa bile aynı sipariş için ikinci rezervasyon oluşamaz (defense in depth).
/// Hem başarılı hem başarısız sonuç domain event üretir; böylece tüm integration event'ler Outbox'tan geçer.
/// </summary>
public sealed class StockReservation : AggregateRoot<Guid>
{
    private readonly List<StockReservationItem> _items = [];

    private StockReservation()
    {
        // EF Core için
    }

    private StockReservation(Guid id, Guid orderId, ReservationStatus status, string? rejectionReason) : base(id)
    {
        OrderId = orderId;
        Status = status;
        RejectionReason = rejectionReason;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid OrderId { get; private set; }

    public ReservationStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public IReadOnlyCollection<StockReservationItem> Items => _items.AsReadOnly();

    public static StockReservation Reserve(Guid orderId, IReadOnlyCollection<ReservationLine> lines)
    {
        var reservation = new StockReservation(Guid.NewGuid(), orderId, ReservationStatus.Reserved, null);

        foreach (var line in lines)
        {
            reservation._items.Add(new StockReservationItem(Guid.NewGuid(), reservation.Id, line.ProductId, line.Quantity));
        }

        reservation.Raise(new StockReservedDomainEvent(reservation.Id, orderId));
        return reservation;
    }

    public static StockReservation Reject(Guid orderId, string reason)
    {
        var reservation = new StockReservation(Guid.NewGuid(), orderId, ReservationStatus.Rejected, reason);
        reservation.Raise(new StockReservationRejectedDomainEvent(reservation.Id, orderId, reason));
        return reservation;
    }
}

public sealed class StockReservationItem : Entity<Guid>
{
    private StockReservationItem()
    {
    }

    internal StockReservationItem(Guid id, Guid reservationId, Guid productId, int quantity) : base(id)
    {
        ReservationId = reservationId;
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid ReservationId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }
}

public sealed record StockReservedDomainEvent(Guid ReservationId, Guid OrderId) : DomainEvent;

public sealed record StockReservationRejectedDomainEvent(Guid ReservationId, Guid OrderId, string Reason) : DomainEvent;
