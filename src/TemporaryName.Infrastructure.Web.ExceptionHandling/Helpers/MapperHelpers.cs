using System;
using Microsoft.AspNetCore.Http;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;

public static class MapperHelpers
{
    internal static string GetSanitizedStackTrace(Exception ex)
    {
        // Potentially sanitize paths or other sensitive info from stack trace if ever exposed,
        // though typically only for dev environments.
        return ex.StackTrace ?? "No stack trace available.";
    }

    internal static object GetRecursiveInnerExceptionDetails(Exception innerException)
    {
        List<object> innerDetails = new();
        Exception? current = innerException;
        int depth = 0;
        const int maxDepth = 5; // Prevent excessively deep recursion

        while (current != null && depth < maxDepth)
        {
            innerDetails.Add(new
            {
                Type = current.GetType().FullName,
                Message = current.Message,
                StackTrace = GetSanitizedStackTrace(current) // Apply same sanitization
            });
            current = current.InnerException;
            depth++;
        }
        if (current != null) // Indicates maxDepth was reached
        {
            innerDetails.Add(new { Message = "Further inner exception details truncated due to depth limit."});
        }
        return innerDetails;
    }

    internal static (int StatusCode, string Title, string TypeSuffix) MapErrorTypeToHttpDetails(Error error)
    {
        // Use error.Code for more specific titles/types if available and meaningful
        string title = error.Code ?? "Domain Error";
        string typeSuffix = !string.IsNullOrWhiteSpace(error.Code)
            ? error.Code.ToLowerInvariant().Replace(".", "-")
            : "domain-error";

        return error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, error.Code ?? "Validation Failure", "validation-failure"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, error.Code ?? "Resource Not Found", "not-found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, error.Code ?? "Conflict Occurred", "conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, error.Code ?? "Unauthorized Access", "unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, error.Code ?? "Access Forbidden", "forbidden"),
            ErrorType.Failure => (StatusCodes.Status400BadRequest, title, typeSuffix), // Or 500 depending on context
            ErrorType.Unexpected => (StatusCodes.Status500InternalServerError, error.Code ?? "Unexpected Domain Error", "unexpected-domain-error"),
            ErrorType.Problem or _ => (StatusCodes.Status500InternalServerError, title, typeSuffix),
        };
    }
}
