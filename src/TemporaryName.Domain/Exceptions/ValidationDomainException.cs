using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class ValidationDomainException : DomainException
{
    /// <summary>
    /// A dictionary where the key is the field/property name (or a general error code)
    /// and the value is an array of error messages for that field/property.
    /// This structure is convenient for populating ProblemDetails validation extensions.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationMessages { get; }

    public ValidationDomainException(IEnumerable<Error> validationErrors)
        : base(CreateSummaryError(validationErrors), validationErrors)
    {
        ValidationMessages = validationErrors
            .GroupBy(e => e.Code) // Group by field name or specific error code from Error.Code
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description ?? "Validation error.").ToArray()
            );
    }
    
    public ValidationDomainException(string message, IEnumerable<Error> validationErrors)
        : base(message, CreateSummaryError(validationErrors), validationErrors)
    {
        ValidationMessages = validationErrors
            .GroupBy(e => e.Code) 
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description ?? "Validation error.").ToArray()
            );
    }

    private static Error CreateSummaryError(IEnumerable<Error> validationErrors)
    {
        if (validationErrors == null || !validationErrors.Any())
        {
            throw new ArgumentException("Validation errors collection cannot be null or empty.", nameof(validationErrors));
        }
        // Create a summary error object. The metadata can hold all individual errors if needed,
        // or the ProblemDetails mapper can use the `Errors` property of DomainException.
        Dictionary<string, object> metadata = validationErrors
            .GroupBy(e => e.Code)
            .ToDictionary(
                g => g.Key,
                g => (object)g.Select(e => e.Description ?? string.Empty).ToArray()
            );

        return new Error(
            "Validation.MultipleFailures",
            "One or more validation errors occurred. See details for more information.",
            ErrorType.Validation,
            metadata
        );
    }
}
