using BuildingBlocks.Abstractions.Domain;

namespace Ordering.Domain.Customers.Events;

public sealed record CustomerCreatedDomainEvent(
    Guid CustomerId,
    string Code,
    string Name,
    string Email) : DomainEvent;

public sealed record CustomerUpdatedDomainEvent(
    Guid CustomerId,
    string Name,
    string Email) : DomainEvent;

public sealed record CustomerArchivedDomainEvent(
    Guid CustomerId) : DomainEvent;

public sealed record CustomerActivatedDomainEvent(
    Guid CustomerId) : DomainEvent;
