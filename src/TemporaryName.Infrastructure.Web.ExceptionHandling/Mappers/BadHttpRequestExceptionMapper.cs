using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class BadHttpRequestExceptionMapper : IExceptionProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;
    public BadHttpRequestExceptionMapper(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
    public Type HandledExceptionType => typeof(BadHttpRequestException);
    public int Order => 280; 

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        BadHttpRequestException? badRequestEx = exception as BadHttpRequestException;

        ProblemDetails problemDetails = new()
        {
            Status = badRequestEx?.StatusCode ?? StatusCodes.Status400BadRequest, 
            Title = "Bad Request",
            Detail = _environment.IsDevelopment() || options.IncludeStackTrace
                ? exception.Message
                : "The server could not understand the request due to invalid syntax or malformed content.",
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "bad-http-request"),
            Instance = httpContext.Request.Path
        };

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}