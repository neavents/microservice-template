using System;
using System.Linq.Expressions;

namespace TemporaryName.Domain.Primitives.Specification.Extensions;

internal static class ExpressionCombiner
{
    public static Expression<Func<T, bool>> CombineAnd<T>(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right)
    {
        if (left == null) return right ?? (x => true); // If left is null, return right (or true if both null)
        if (right == null) return left;                // If right is null, return left

        ParameterExpression param = left.Parameters[0];
        Expression rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], param);
        Expression combinedBody = Expression.AndAlso(left.Body, rightBody);

        return Expression.Lambda<Func<T, bool>>(combinedBody, param);
    }

     public static Expression<Func<T, bool>> CombineOr<T>(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right)
    {
        if (left == null) return right ?? (x => false); // If left is null, return right (or false if both null)
        if (right == null) return left;                 // If right is null, return left

        ParameterExpression param = left.Parameters[0];
        Expression rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], param);
        Expression combinedBody = Expression.OrElse(left.Body, rightBody);

        return Expression.Lambda<Func<T, bool>>(combinedBody, param);
    }

     public static Expression<Func<T, bool>> CombineNot<T>(Expression<Func<T, bool>>? expression)
     {
         if (expression == null) return x => true; // Not of 'null' (always true?) is debatable, maybe throw?

         ParameterExpression param = expression.Parameters[0];
         Expression combinedBody = Expression.Not(expression.Body);

         return Expression.Lambda<Func<T, bool>>(combinedBody, param);
     }
}
