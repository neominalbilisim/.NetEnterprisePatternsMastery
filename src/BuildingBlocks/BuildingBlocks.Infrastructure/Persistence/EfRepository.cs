using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// IRepository portunun EF Core adapter'ı. Servisler aggregate'e özel repository'lerini bu sınıftan türetir.
/// SaveChanges çağırmaz: commit kararı UnitOfWork'e aittir.
/// </summary>
public abstract class EfRepository<TAggregate, TId>(DbContext context) : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    protected DbContext Context { get; } = context;

    protected DbSet<TAggregate> Set => Context.Set<TAggregate>();

    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public virtual Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        Apply(specification).FirstOrDefaultAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        await Apply(specification).ToListAsync(cancellationToken);

    public virtual Task<int> CountAsync(
        ISpecification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.GetQuery(Set.AsQueryable(), specification, criteriaOnly: true).CountAsync(cancellationToken);

    public virtual Task<bool> AnyAsync(
        ISpecification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.GetQuery(Set.AsQueryable(), specification, criteriaOnly: true).AnyAsync(cancellationToken);

    public virtual void Add(TAggregate aggregate) => Set.Add(aggregate);

    public virtual void Update(TAggregate aggregate) => Set.Update(aggregate);

    public virtual void Remove(TAggregate aggregate) => Set.Remove(aggregate);

    protected IQueryable<TAggregate> Apply(ISpecification<TAggregate> specification) =>
        SpecificationEvaluator.GetQuery(Set.AsQueryable(), specification);
}
