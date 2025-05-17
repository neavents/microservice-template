using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ValidationDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(ValidationDomainException);
    public int Order => 90; // Higher precedence than the generic DomainExceptionMapper

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);
        
        if (exception is not ValidationDomainException validationException)
        {
            throw new ArgumentException($"Exception is not of type {nameof(ValidationDomainException)}.", nameof(exception));
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status400BadRequest, // Or StatusCodes.Status422UnprocessableEntity
            Title = validationException.ErrorDetails.Code ?? "Validation Failed",
            Detail = validationException.ErrorDetails.Description ?? "One or more validation errors occurred.",
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "validation-failure"),
            Instance = httpContext.Request.Path
        };

        // Add structured validation errors to the extensions
        if (validationException.Errors != null && validationException.Errors.Any())
        {
            problemDetails.Extensions[ProblemDetailsConstants.ValidationErrorsExtensionKey] = validationException.Errors;
        }
        else if (validationException.ErrorDetails.Metadata != null && validationException.ErrorDetails.Metadata.Any())
        {
            // Fallback if structured Errors property is not populated but metadata has validation info
             problemDetails.Extensions[ProblemDetailsConstants.ValidationErrorsExtensionKey] = validationException.ErrorDetails.Metadata;
        }


        // No stack trace for validation errors by default, as they are expected user input issues.
        // Can be added if options.IncludeStackTrace is true and it's deemed useful for debugging validation logic.
        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = exception.StackTrace ?? "No stack trace available.";
        }


        return problemDetails;
    }
}
