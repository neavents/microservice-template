using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class RateLimitDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(RateLimitDomainException);
    public int Order => 85; // High precedence

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not RateLimitDomainException rateLimitException)
        {
            throw new InvalidOperationException($"Mapper {nameof(RateLimitDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = rateLimitException.ErrorDetails.Code ?? "Rate Limit Exceeded",
            Detail = rateLimitException.ErrorDetails.Description ?? rateLimitException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "rate-limit-exceeded"),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["resourceOrOperation"] = rateLimitException.ResourceOrOperation;
        if (rateLimitException.RetryAfter.HasValue)
        {
            // Add Retry-After header to the actual HTTP response
            httpContext.Response.Headers["Retry-After"] = rateLimitException.RetryAfter.Value.TotalSeconds.ToString("F0");
            problemDetails.Extensions["retryAfterSeconds"] = rateLimitException.RetryAfter.Value.TotalSeconds;
        }
        
        if (rateLimitException.ErrorDetails.Metadata != null && rateLimitException.ErrorDetails.Metadata.Any())
        {
             foreach(var meta in rateLimitException.ErrorDetails.Metadata)
            {
                // Avoid overwriting retryAfterSeconds if already set from specific property
                if(meta.Key.Equals("retryAfterSeconds", StringComparison.OrdinalIgnoreCase) && problemDetails.Extensions.ContainsKey("retryAfterSeconds")) continue;
                problemDetails.Extensions.TryAdd(meta.Key, meta.Value);
            }
        }

        // Stack trace usually not relevant for 429 to the client.
        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
