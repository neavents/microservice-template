using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ConfigurationDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(ConfigurationDomainException);
    public int Order => 160; // Server-side configuration issue

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not ConfigurationDomainException configException)
        {
            throw new InvalidOperationException($"Mapper {nameof(ConfigurationDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError, // Critical server setup issue
            Title = configException.ErrorDetails.Code ?? "Server Configuration Error",
            Detail = configException.ErrorDetails.Description ?? "A critical server configuration is missing or invalid, preventing the operation.",
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "server-configuration-error"),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["configurationKey"] = configException.ConfigurationKey;
        
        if (configException.ErrorDetails.Metadata != null && configException.ErrorDetails.Metadata.Any())
        {
             foreach(var meta in configException.ErrorDetails.Metadata)
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
