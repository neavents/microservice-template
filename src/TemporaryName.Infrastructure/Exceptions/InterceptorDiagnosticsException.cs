using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.Exceptions;

/// <summary>
/// Represents an exception that occurs within an interceptor, often related to its internal processing
/// such as serialization or configuration issues, rather than the intercepted method's business logic.
/// </summary>
public class InterceptorDiagnosticsException : Exception
{
    /// <summary>
    /// Gets the specific error details associated with this interceptor exception.
    /// </summary>
    public Error ErrorDetails { get; }

    /// <summary>
    /// Gets the name of the interceptor where the exception originated.
    /// </summary>
    public string InterceptorName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptorDiagnosticsException"/> class.
    /// </summary>
    /// <param name="interceptorName">The name of the interceptor.</param>
    /// <param name="error">The structured error details.</param>
    public InterceptorDiagnosticsException(string interceptorName, Error error)
        : base(error.Description ?? $"An error occurred in interceptor '{interceptorName}': {error.Code}")
    {
        InterceptorName = interceptorName;
        ErrorDetails = error;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptorDiagnosticsException"/> class with a specific message and inner exception.
    /// </summary>
    /// <param name="interceptorName">The name of the interceptor.</param>
    /// <param name="error">The structured error details.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InterceptorDiagnosticsException(string interceptorName, Error error, Exception innerException)
        : base(error.Description ?? $"An error occurred in interceptor '{interceptorName}': {error.Code}", innerException)
    {
        InterceptorName = interceptorName;
        ErrorDetails = error;
    }

    /// <summary>
    /// Creates a new InterceptorDiagnosticsException for a serialization failure.
    /// </summary>
    public static InterceptorDiagnosticsException SerializationFailure(
        string interceptorName,
        string targetDescription,
        string? specificItemName, 
        Exception innerException,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var error = new Error(
            code: $"Interceptor.{interceptorName}.SerializationFailure",
            description: $"Failed to serialize {targetDescription}" + (string.IsNullOrWhiteSpace(specificItemName) ? "" : $" for '{specificItemName}'") + $" in interceptor '{interceptorName}'.",
            type: ErrorType.Unexpected, 
            initialMetadata: metadata ?? new Dictionary<string, object?> { { "TargetDescription", targetDescription }, { "ItemName", specificItemName } }
        );
        return new InterceptorDiagnosticsException(interceptorName, error, innerException);
    }

     /// <summary>
    /// Creates a new InterceptorDiagnosticsException for a configuration issue.
    /// </summary>
    public static InterceptorDiagnosticsException ConfigurationError(
        string interceptorName,
        string configurationDetails,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var error = new Error(
            code: $"Interceptor.{interceptorName}.ConfigurationError",
            description: $"Configuration error in interceptor '{interceptorName}': {configurationDetails}",
            type: ErrorType.Problem, 
            initialMetadata: metadata ?? new Dictionary<string, object?> { { "ConfigurationDetails", configurationDetails } }
        );
        return new InterceptorDiagnosticsException(interceptorName, error);
    }
}
