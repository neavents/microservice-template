using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class KeyNotFoundExceptionMapper : IExceptionProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;
    public KeyNotFoundExceptionMapper(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
    public Type HandledExceptionType => typeof(KeyNotFoundException);
    public int Order => 270;

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Key Not Found",
            Detail = _environment.IsDevelopment() || options.IncludeStackTrace
                ? exception.Message
                : "The specified key was not found or does not correspond to an existing resource.",
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "key-not-found"),
            Instance = httpContext.Request.Path
        };

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}