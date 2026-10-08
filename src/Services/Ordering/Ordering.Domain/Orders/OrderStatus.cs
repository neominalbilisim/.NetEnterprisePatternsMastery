namespace Ordering.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,   // Oluşturuldu, stok rezervasyonu bekleniyor
    Confirmed = 2, // Stok rezerve edildi
    Rejected = 3   // Stok rezerve edilemedi
}
