using BuildingBlocks.Abstractions.Persistence;

namespace Inventory.Domain.Products;

public interface IProductRepository : IRepository<Product, Guid>
{
}
