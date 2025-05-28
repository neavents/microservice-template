using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ExternalServiceDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;
    public ExternalServiceDomainExceptionMapper(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Type HandledExceptionType => typeof(ExternalServiceDomainException);
    public int Order => 150; // Domain-related, but potentially less specific than direct user input errors

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);
     
        if (exception is not ExternalServiceDomainException serviceException)
        {
            throw new InvalidOperationException($"Mapper {nameof(ExternalServiceDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        // Typically, errors with external services result in a Bad Gateway or Service Unavailable
        // if the external service is critical for the current operation.
        int statusCode = StatusCodes.Status502BadGateway;
        string title = serviceException.ErrorDetails.Code ?? $"External Service Error: {serviceException.ServiceName}";
        string typeSuffix = $"external-service-failure{(string.IsNullOrWhiteSpace(serviceException.ServiceName) ? "" : $"-{serviceException.ServiceName.ToLowerInvariant()}")}";

        // If the error type suggests a client-side issue with how the external service was called (e.g., bad input to it),
        // it might be a 400, but that's less common for this exception type's intent.
        // For now, assume it's a server-side or connectivity problem with the external dependency.

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() || options.IncludeStackTrace
                ? serviceException.ErrorDetails.Description ?? serviceException.Message
                : $"An issue occurred while communicating with an external service ({serviceException.ServiceName}). Please try again later.",
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, typeSuffix),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["externalServiceName"] = serviceException.ServiceName;
        if (!string.IsNullOrWhiteSpace(serviceException.OperationName))
        {
            problemDetails.Extensions["externalOperationName"] = serviceException.OperationName;
        }
        
        if (serviceException.ErrorDetails.Metadata != null && serviceException.ErrorDetails.Metadata.Any())
        {
             foreach(var meta in serviceException.ErrorDetails.Metadata)
            {
                problemDetails.Extensions.TryAdd(meta.Key, meta.Value);
            }
        }

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
            if (options.IncludeInnerException && exception.InnerException != null)
            {
                problemDetails.Extensions[ProblemDetailsConstants.InnerExceptionExtensionKey] =
                    MapperHelpers.GetRecursiveInnerExceptionDetails(exception.InnerException);
            }
        }
        return problemDetails;
    }
}
