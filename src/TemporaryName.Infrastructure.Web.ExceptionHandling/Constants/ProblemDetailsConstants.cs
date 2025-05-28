using System;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;

public static class ProblemDetailsConstants
{
    /// <summary>
    /// The key for the trace/correlation ID extension in ProblemDetails.
    /// </summary>
    public const string TraceIdExtensionKey = "traceId";

    /// <summary>
    /// The key for validation errors extension in ProblemDetails, typically holding a dictionary of field errors.
    /// </summary>
    public const string ValidationErrorsExtensionKey = "errors";

    /// <summary>
    /// The key for generic error metadata from SharedKernel.Error.
    /// </summary>
    public const string ErrorMetadataExtensionKey = "errorMetadata";

    /// <summary>
    /// The key for the stack trace extension in ProblemDetails (usually for development).
    /// </summary>
    public const string StackTraceExtensionKey = "stackTrace";

    /// <summary>
    /// The key for inner exception details extension in ProblemDetails (usually for development).
    /// </summary>
    public const string InnerExceptionExtensionKey = "innerException";
}
