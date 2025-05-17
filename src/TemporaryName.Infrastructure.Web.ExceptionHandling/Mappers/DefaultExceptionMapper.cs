using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class DefaultExceptionMapper : IExceptionProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;

    public DefaultExceptionMapper(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Type HandledExceptionType => typeof(Exception);
    public int Order => 1000;

    internal static object GetRecursiveInnerExceptionDetails(Exception? innerException, object includeStackTrace)
    {
        throw new NotImplementedException();
    }

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
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected server error occurred.",
            Detail = options.IncludeStackTrace || _environment.IsDevelopment()
                ? exception.Message // Show actual message in dev or if stack trace is enabled
                : "An internal error occurred while processing your request. Please try again later.",
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, "internal-server-error"),
            Instance = httpContext.Request.Path
        };

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
            if (options.IncludeInnerException && exception.InnerException is not null)
            {
                problemDetails.Extensions[ProblemDetailsConstants.InnerExceptionExtensionKey] =
                    MapperHelpers.GetRecursiveInnerExceptionDetails(exception.InnerException);
            }
        }

        return problemDetails;
    }
    
    
}
