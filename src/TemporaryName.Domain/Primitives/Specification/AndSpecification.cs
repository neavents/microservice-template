using System;
using TemporaryName.Domain.Primitives.Specification.Extensions;

namespace TemporaryName.Domain.Primitives.Specification;

internal sealed class AndSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
        Criteria = ExpressionCombiner.CombineAnd(left.Criteria, right.Criteria);
        // Combine includes, take lowest paging Take, highest Skip? Combine OrderBy? - Needs defined strategy
        // Simple approach: prioritize left spec for non-criteria properties
        _includes.AddRange(left.Includes);
        _includes.AddRange(right.Includes); // Might have duplicates,Distinct needed later
        _includeStrings.AddRange(left.IncludeStrings);
        _includeStrings.AddRange(right.IncludeStrings);
        OrderBy = left.OrderBy ?? right.OrderBy;
        OrderByDescending = left.OrderByDescending ?? right.OrderByDescending;
        if (left.IsPagingEnabled || right.IsPagingEnabled)
        {
            // Example strategy: Take the stricter limit
            ApplyPaging(Math.Max(left.Skip, right.Skip), Math.Min(left.Take > 0 ? left.Take : int.MaxValue, right.Take > 0 ? right.Take : int.MaxValue));
        }
    }
}
