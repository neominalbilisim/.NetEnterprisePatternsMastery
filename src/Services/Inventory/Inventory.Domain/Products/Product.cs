using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Abstractions.Results;

namespace Inventory.Domain.Products;

/// <summary>
/// Product aggregate root. Stok tutarlılığı optimistic concurrency ile korunur:
/// Version alanı PostgreSQL'in xmin sistem kolonuna eşlenir; aynı ürünü eşzamanlı güncelleyen iki işlemden
/// biri DbUpdateConcurrencyException alır ve CAP tarafından yeniden denenir.
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    private Product()
    {
        // EF Core için
    }

    private Product(Guid id, string sku, string name, decimal unitPrice, int availableQuantity) : base(id)
    {
        Sku = sku;
        Name = name;
        UnitPrice = unitPrice;
        AvailableQuantity = availableQuantity;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Sku { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public decimal UnitPrice { get; private set; }

    public int AvailableQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    /// <summary>Concurrency token (PostgreSQL xmin).</summary>
    public uint Version { get; private set; }

    public static Result<Product> Create(string sku, string name, decimal unitPrice, int initialStock, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) return ProductErrors.SkuRequired;
        if (string.IsNullOrWhiteSpace(name)) return ProductErrors.NameRequired;
        if (unitPrice <= 0) return ProductErrors.InvalidPrice;
        if (initialStock < 0) return ProductErrors.InvalidQuantity;

        return new Product(id ?? Guid.NewGuid(), sku.Trim().ToUpperInvariant(), name.Trim(), unitPrice, initialStock);
    }

    public bool CanReserve(int quantity) => quantity > 0 && AvailableQuantity >= quantity;

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            return ProductErrors.InvalidQuantity;
        }

        if (AvailableQuantity < quantity)
        {
            return ProductErrors.InsufficientStock(Sku, quantity, AvailableQuantity);
        }

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
        return Result.Success();
    }
}
