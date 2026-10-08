using System.Linq.Expressions;

namespace BuildingBlocks.Abstractions.Specifications;

/// <summary>
/// Specification Pattern: Sorgu kriterlerini (filtre, include, sıralama, sayfalama)
/// isimlendirilmiş, yeniden kullanılabilir ve test edilebilir nesnelere kapsüller.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    Expression<Func<T, object>>? OrderBy { get; }

    Expression<Func<T, object>>? OrderByDescending { get; }

    int? Skip { get; }

    int? Take { get; }

    bool IsNoTracking { get; }

    /// <summary>Kuralı bellekteki bir nesne üzerinde de çalıştırabilmeyi sağlar (domain doğrulamaları, testler).</summary>
    bool IsSatisfiedBy(T entity);
}
