using MediatR;

namespace BuildingBlocks.Abstractions.Domain;

/// <summary>
/// Domain içinde gerçekleşmiş, iş açısından anlamlı bir olayı temsil eder.
/// Domain event'ler aynı process ve aynı transaction içinde (in-process) MediatR ile dispatch edilir.
/// Servis sınırını aşması gereken bilgiler için Integration Event kullanılır.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
