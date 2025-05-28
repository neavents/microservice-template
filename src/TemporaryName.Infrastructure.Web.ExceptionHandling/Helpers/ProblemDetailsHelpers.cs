using System;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;

public static class ProblemDetailsHelpers
{
    internal static int GetInheritanceDepth(Type type)
    {
        int depth = 0;
        Type? current = type;
        while (current != null)
        {
            depth++;
            current = current.BaseType;
        }
        return depth;
    }

    internal static string CombineProblemTypeUri(string? baseUri, string specificType)
    {
        if (string.IsNullOrWhiteSpace(baseUri))
        {
            return specificType;
        }
        string BUri = baseUri.TrimEnd('/');
        string SType = specificType.TrimStart('/');
        return $"{BUri}/{SType}";
    }
}
