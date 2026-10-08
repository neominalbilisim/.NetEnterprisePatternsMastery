using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Demo verisi. Sabit Id'ler Postman collection'daki değişkenlerle eşleşir.
/// Monitör stoğu bilerek düşük (3) tutulmuştur: yetersiz stok ve eşzamanlılık senaryoları için.
/// </summary>
public static class InventorySeeder
{
    public static readonly Guid KeyboardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid MouseId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid MonitorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(InventoryDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new[]
        {
            Product.Create("KB-001", "Mechanical Keyboard", 2499.90m, 50, KeyboardId),
            Product.Create("MS-001", "Wireless Mouse", 899.50m, 100, MouseId),
            Product.Create("MN-001", "27\" 4K Monitor", 8999.00m, 3, MonitorId)
        };

        context.Products.AddRange(products.Where(r => r.IsSuccess).Select(r => r.Value));
        await context.SaveChangesAsync(cancellationToken);
    }
}
