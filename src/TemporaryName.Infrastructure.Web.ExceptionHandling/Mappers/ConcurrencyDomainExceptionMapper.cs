using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ConcurrencyDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(ConcurrencyDomainException);
    public int Order => 96; // Specific domain exception

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);
        
        if (exception is not ConcurrencyDomainException concurrencyException)
        {
            throw new InvalidOperationException($"Mapper {nameof(ConcurrencyDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status409Conflict, // Standard for concurrency issues
            Title = concurrencyException.ErrorDetails.Code ?? "Concurrency Conflict",
            Detail = concurrencyException.ErrorDetails.Description ?? concurrencyException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "concurrency-conflict"),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["resourceType"] = concurrencyException.ResourceType;
        problemDetails.Extensions["resourceIdentifier"] = concurrencyException.ResourceIdentifier?.ToString();
        
        if (concurrencyException.ErrorDetails.Metadata != null && concurrencyException.ErrorDetails.Metadata.Any())
        {
            foreach(var meta in concurrencyException.ErrorDetails.Metadata)
            {
                problemDetails.Extensions.TryAdd(meta.Key, meta.Value);
            }
        }


        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
