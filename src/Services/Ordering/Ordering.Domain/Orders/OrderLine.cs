namespace Ordering.Domain.Orders;

/// <summary>Sipariş oluşturma girdisi (değer nesnesi). Fiyat istemciden değil, Inventory servisinden gelir.</summary>
public sealed record OrderLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
