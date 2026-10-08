using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application;

public static class DependencyInjection
{
    /// <summary>
    /// MediatR (CQRS), pipeline behavior'lar ve FluentValidation kayıtlarını yapar.
    /// </summary>
    public static IServiceCollection AddApplicationCore(this IServiceCollection services, Assembly applicationAssembly)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);

            // SIRA ÖNEMLİDİR: İlk eklenen en dışta çalışır.
            // Logging -> Validation -> Transaction -> Idempotency -> Inbox -> Handler
            // Behavior'lar generic kısıtlarla hedeflenir (örn. Transaction sadece IBaseCommand için);
            // kısıtı sağlamayan istekler için DI container ilgili behavior'ı otomatik olarak atlar.
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
            configuration.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
            configuration.AddOpenBehavior(typeof(InboxBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        return services;
    }
}
