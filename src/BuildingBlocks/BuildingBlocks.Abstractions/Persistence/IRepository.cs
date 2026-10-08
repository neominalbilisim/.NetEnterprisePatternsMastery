using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Abstractions.Specifications;

namespace BuildingBlocks.Abstractions.Persistence;

/// <summary>
/// Repository Pattern (driven port). Yalnızca aggregate root'lar için tanımlanır.
/// Bilinçli olarak IQueryable döndürmez: sorgu mantığı Specification'larda kapsüllenir,
/// böylece soyutlama sızdırılmaz (leaky abstraction).
/// SaveChanges burada YOKTUR: commit kararı Unit of Work'e aittir.
/// </summary>
public interface IRepository<TAggregate, in TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<TAggregate?> FirstOrDefaultAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
