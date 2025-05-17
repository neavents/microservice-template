using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ConflictDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(ConflictDomainException);
    public int Order => 94; // More specific than DomainExceptionMapper

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not ConflictDomainException conflictException)
        {
            throw new InvalidOperationException($"Mapper {nameof(ConflictDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status409Conflict,
            Title = conflictException.ErrorDetails.Code ?? "Resource Conflict",
            Detail = conflictException.ErrorDetails.Description ?? conflictException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "resource-conflict"),
            Instance = httpContext.Request.Path
        };
        
        if (conflictException.ErrorDetails.Metadata != null && conflictException.ErrorDetails.Metadata.Any())
        {
            problemDetails.Extensions[ProblemDetailsConstants.ErrorMetadataExtensionKey] = conflictException.ErrorDetails.Metadata;
        }

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
