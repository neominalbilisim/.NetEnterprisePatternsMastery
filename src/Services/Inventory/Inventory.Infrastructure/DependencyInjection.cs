using BuildingBlocks.Infrastructure;
using Inventory.Application.Ports;
using Inventory.Domain.Products;
using Inventory.Domain.Reservations;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.ReadModels;
using Inventory.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException("Connection string 'InventoryDb' is missing.");

        services.AddPersistenceCore<InventoryDbContext>(connectionString);
        services.AddCapMessaging<InventoryDbContext>(configuration, groupName: "inventory");

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddScoped<IInventoryReadRepository, DapperInventoryReadRepository>();

        return services;
    }
}
