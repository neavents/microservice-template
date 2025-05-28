using System;
using System.Linq.Expressions;

namespace TemporaryName.Domain.Primitives.Specification.Extensions;

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;
    private readonly ParameterExpression _replacement;

    private ParameterReplacer(ParameterExpression parameter, ParameterExpression replacement)
    {
        _parameter = parameter;
        _replacement = replacement;
    }

    public static Expression Replace(Expression expression, ParameterExpression parameter, ParameterExpression replacement)
    {
        return new ParameterReplacer(parameter, replacement).Visit(expression);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _parameter ? _replacement : base.VisitParameter(node);
    }
}
