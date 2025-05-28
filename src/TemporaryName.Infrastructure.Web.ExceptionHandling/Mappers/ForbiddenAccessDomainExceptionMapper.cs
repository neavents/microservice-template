using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ForbiddenAccessDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(ForbiddenAccessDomainException);
    public int Order => 93; // Higher precedence than generic DomainExceptionMapper

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not ForbiddenAccessDomainException forbiddenException)
        {
            throw new InvalidOperationException($"Mapper {nameof(ForbiddenAccessDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status403Forbidden,
            Title = forbiddenException.ErrorDetails.Code ?? "Access Forbidden",
            Detail = forbiddenException.ErrorDetails.Description ?? forbiddenException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "access-forbidden"),
            Instance = httpContext.Request.Path
        };

        if (forbiddenException.ErrorDetails.Metadata != null && forbiddenException.ErrorDetails.Metadata.Any())
        {
            problemDetails.Extensions[ProblemDetailsConstants.ErrorMetadataExtensionKey] = forbiddenException.ErrorDetails.Metadata;
        }

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
