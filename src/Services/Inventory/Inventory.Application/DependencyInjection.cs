using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services) =>
        services.AddApplicationCore(typeof(DependencyInjection).Assembly);
}
