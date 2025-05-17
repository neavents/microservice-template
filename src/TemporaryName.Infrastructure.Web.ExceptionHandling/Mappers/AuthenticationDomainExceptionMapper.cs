using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class AuthenticationDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(AuthenticationDomainException);
    public int Order => 80; // High precedence for auth issues

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not AuthenticationDomainException authException)
        {
            throw new InvalidOperationException($"Mapper {nameof(AuthenticationDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = authException.ErrorDetails.Code ?? "Authentication Failed",
            Detail = authException.ErrorDetails.Description ?? authException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "authentication-failure"),
            Instance = httpContext.Request.Path
        };
        
        if (authException.ErrorDetails.Metadata != null && authException.ErrorDetails.Metadata.Any())
        {
             foreach(var meta in authException.ErrorDetails.Metadata)
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