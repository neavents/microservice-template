using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TemporaryName.Domain.Primitives.Specification;

public interface ISpecification<T>
{
    // Primary filtering criterion
    Expression<Func<T, bool>>? Criteria { get; }

    // Includes for eager loading related entities
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }

    // Ordering criteria
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    // Paging criteria
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }

    // Caching strategy (optional, see discussion below)
    // string? CacheKey { get; }
    // bool CacheEnabled { get; }
}