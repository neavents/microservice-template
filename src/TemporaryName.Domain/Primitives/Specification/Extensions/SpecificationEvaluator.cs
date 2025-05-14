using System;

namespace TemporaryName.Domain.Primitives.Specification.Extensions;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> specification)
        where TEntity : class // Can be applied to any class EF Core can query
    {
        IQueryable<TEntity> query = inputQuery;

        // Apply criteria (WHERE clause)
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes (Eager Loading)
        // Combine expression-based and string-based includes
        query = specification.Includes.Aggregate(query,
                            (current, include) => current.Include(include));

        query = specification.IncludeStrings.Aggregate(query,
                            (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }
        // Add ThenBy logic here if ISpecification supports it

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            // Ensure Skip is non-negative, Take is positive
            int skip = Math.Max(0, specification.Skip);
            int take = specification.Take > 0 ? specification.Take : int.MaxValue; // Or handle invalid Take?
            query = query.Skip(skip).Take(take);
        }

        return query;
    }
}
