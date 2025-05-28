using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class NotFoundDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(NotFoundDomainException);
    public int Order => 95; // More specific than DomainExceptionMapper

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not NotFoundDomainException notFoundException)
        {
            throw new InvalidOperationException($"Mapper {nameof(NotFoundDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status404NotFound,
            Title = notFoundException.ErrorDetails.Code ?? "Resource Not Found",
            Detail = notFoundException.ErrorDetails.Description ?? notFoundException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "resource-not-found"),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["resourceName"] = notFoundException.ResourceName;
        problemDetails.Extensions["resourceIdentifier"] = notFoundException.ResourceIdentifier?.ToString();

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
