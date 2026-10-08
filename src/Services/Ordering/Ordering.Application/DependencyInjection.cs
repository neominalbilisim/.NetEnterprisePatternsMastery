using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services) =>
        services.AddApplicationCore(typeof(DependencyInjection).Assembly);
}
