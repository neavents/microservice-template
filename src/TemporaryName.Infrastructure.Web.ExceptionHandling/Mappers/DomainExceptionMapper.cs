using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Primitives;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class DomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(DomainException);
    public int Order => 100; // Before DefaultExceptionMapper, after more specific domain mappers

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);
        
        if (exception is not DomainException domainException)
        {
            // This should not happen if the factory calls the correct mapper
            throw new ArgumentException($"Exception is not of type {nameof(DomainException)}.", nameof(exception));
        }

        Error errorDetails = domainException.ErrorDetails;
        (int statusCode, string title, string typeSuffix) = MapperHelpers.MapErrorTypeToHttpDetails(errorDetails);

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = errorDetails.Description ?? domainException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, typeSuffix),
            Instance = httpContext.Request.Path
        };

        if (errorDetails.Metadata != null && errorDetails.Metadata.Any())
        {
            problemDetails.Extensions[ProblemDetailsConstants.ErrorMetadataExtensionKey] = errorDetails.Metadata;
        }
        
        // Optionally add stack trace for domain exceptions in dev if configured
        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = exception.StackTrace ?? "No stack trace available.";
             if (options.IncludeInnerException && exception.InnerException != null)
            {
                problemDetails.Extensions[ProblemDetailsConstants.InnerExceptionExtensionKey] =
                    MapperHelpers.GetRecursiveInnerExceptionDetails(exception.InnerException); // Reuse logic
            }
        }

        return problemDetails;
    }

    
}
