using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Customers.Events;

namespace Ordering.Application.Customers.EventHandlers;

/// <summary>
/// CustomerCreatedDomainEvent handler. Müşteri oluşturulduğunda tetiklenir.
/// NOT: IntegrationEvent göndermeyi; yalnızca logging ve iş kurallarını işler.
/// </summary>
internal sealed class CustomerCreatedDomainEventHandler(ILogger<CustomerCreatedDomainEventHandler> logger)
    : INotificationHandler<CustomerCreatedDomainEvent>
{
    public Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Customer {CustomerId} created with code {Code} and name {Name}. Email: {Email}",
            notification.CustomerId,
            notification.Code,
            notification.Name,
            notification.Email);

        // Reusable: E-posta gönderme, notifikasyon sırası oluşturma, raporlama vb.
        // IntegrationEvent göndermeden domain event'i local olarak işlenir.

        return Task.CompletedTask;
    }
}

/// <summary>
/// CustomerUpdatedDomainEvent handler. Müşteri bilgileri güncellendiğinde tetiklenir.
/// </summary>
internal sealed class CustomerUpdatedDomainEventHandler(ILogger<CustomerUpdatedDomainEventHandler> logger)
    : INotificationHandler<CustomerUpdatedDomainEvent>
{
    public Task Handle(CustomerUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Customer {CustomerId} updated with name {Name} and email {Email}",
            notification.CustomerId,
            notification.Name,
            notification.Email);

        return Task.CompletedTask;
    }
}

/// <summary>
/// CustomerArchivedDomainEvent handler. Müşteri arşivlendiğinde tetiklenir.
/// </summary>
internal sealed class CustomerArchivedDomainEventHandler(ILogger<CustomerArchivedDomainEventHandler> logger)
    : INotificationHandler<CustomerArchivedDomainEvent>
{
    public Task Handle(CustomerArchivedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Customer {CustomerId} has been archived",
            notification.CustomerId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// CustomerActivatedDomainEvent handler. Müşteri aktive edildiğinde tetiklenir.
/// </summary>
internal sealed class CustomerActivatedDomainEventHandler(ILogger<CustomerActivatedDomainEventHandler> logger)
    : INotificationHandler<CustomerActivatedDomainEvent>
{
    public Task Handle(CustomerActivatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Customer {CustomerId} has been activated",
            notification.CustomerId);

        return Task.CompletedTask;
    }
}
