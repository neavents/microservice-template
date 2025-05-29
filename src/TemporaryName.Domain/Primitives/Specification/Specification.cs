using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TemporaryName.Domain.Primitives.Specification;

public abstract class Specification<T> : ISpecification<T>
{
    public virtual Expression<Func<T, bool>>? Criteria { get; protected set; }

    protected readonly List<Expression<Func<T, object>>> _includes = [];
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    protected readonly List<string> _includeStrings = [];
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    public int Take { get; private set; } = -1; // Use -1 to indicate not set
    public int Skip { get; private set; } = 0;
    public bool IsPagingEnabled => Take > 0;

    // public string? CacheKey { get; protected set; }
    // public bool CacheEnabled { get; protected set; }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
    }

    protected virtual void AddInclude(string includeString)
    {
        _includeStrings.Add(includeString);
    }

    protected virtual void ApplyPaging(int skip, int take)
    {
        if (take <= 0) throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip), "Skip cannot be negative.");

        Skip = skip;
        Take = take;
    }

    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
        OrderByDescending = null;
    }

    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
        OrderBy = null;
    }

    // --- Combinators ---
    public Specification<T> And(ISpecification<T> specification) =>
        new AndSpecification<T>(this, specification);

    // public Specification<T> Or(ISpecification<T> specification) =>
    //     new OrSpecification<T>(this, specification);

    // public Specification<T> Not() =>
    //     new NotSpecification<T>(this);
}

