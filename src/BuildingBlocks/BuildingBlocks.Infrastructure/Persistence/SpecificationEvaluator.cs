using BuildingBlocks.Abstractions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Specification nesnesini EF Core IQueryable'ına uygular. Soyutlama (ISpecification) Abstractions'ta,
/// EF Core'a özgü çeviri burada, altyapı katmanında durur.
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> source, ISpecification<T> specification, bool criteriaOnly = false)
        where T : class
    {
        var query = source;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        // Count/Any gibi sorgularda include, sıralama ve sayfalama gereksizdir.
        if (criteriaOnly)
        {
            return query;
        }

        if (specification.IsNoTracking)
        {
            query = query.AsNoTracking();
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
