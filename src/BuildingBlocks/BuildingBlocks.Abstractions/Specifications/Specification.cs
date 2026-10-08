using System.Linq.Expressions;

namespace BuildingBlocks.Abstractions.Specifications;

/// <summary>
/// Specification'lar için temel sınıf. Kriterler Expression olarak tutulur; böylece EF Core
/// tarafından SQL'e çevrilebilir (client-side evaluation riskine karşı).
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private Func<T, bool>? _compiledCriteria;

    protected Specification(Expression<Func<T, bool>>? criteria = null) => Criteria = criteria;

    public Expression<Func<T, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    public Expression<Func<T, object>>? OrderBy { get; private set; }

    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool IsNoTracking { get; private set; }

    public bool IsSatisfiedBy(T entity)
    {
        if (Criteria is null)
        {
            return true;
        }

        _compiledCriteria ??= Criteria.Compile();
        return _compiledCriteria(entity);
    }

    /// <summary>İki specification'ı AND ile birleştirir (birleştirilebilirlik).</summary>
    public Specification<T> And(Specification<T> other) =>
        new CombinedSpecification(this, other, ExpressionCombiner.AndAlso(Criteria, other.Criteria));

    /// <summary>İki specification'ı OR ile birleştirir.</summary>
    public Specification<T> Or(Specification<T> other) =>
        new CombinedSpecification(this, other, ExpressionCombiner.OrElse(Criteria, other.Criteria));

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => _includes.Add(includeExpression);

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescending) =>
        OrderByDescending = orderByDescending;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>Sadece okuma amaçlı sorgularda change tracking maliyetini kaldırır.</summary>
    protected void ApplyNoTracking() => IsNoTracking = true;

    /// <summary>And/Or ile oluşturulan birleşik specification.</summary>
    private sealed class CombinedSpecification : Specification<T>
    {
        public CombinedSpecification(Specification<T> left, Specification<T> right, Expression<Func<T, bool>>? criteria)
            : base(criteria)
        {
            foreach (var include in left.Includes.Concat(right.Includes))
            {
                AddInclude(include);
            }

            // Sıralama ve sayfalama sol taraftaki specification'dan devralınır.
            if (left.OrderBy is not null) ApplyOrderBy(left.OrderBy);
            if (left.OrderByDescending is not null) ApplyOrderByDescending(left.OrderByDescending);
            if (left.Skip.HasValue && left.Take.HasValue) ApplyPaging(left.Skip.Value, left.Take.Value);
            if (left.IsNoTracking || right.IsNoTracking) ApplyNoTracking();
        }
    }
}
