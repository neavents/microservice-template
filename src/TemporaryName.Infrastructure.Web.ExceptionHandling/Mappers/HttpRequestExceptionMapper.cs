using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class HttpRequestExceptionMapper : IExceptionProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;
    public HttpRequestExceptionMapper(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Type HandledExceptionType => typeof(HttpRequestException);
    public int Order => 230;

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        HttpRequestException? httpReqEx = exception as HttpRequestException;

        int statusCode = StatusCodes.Status502BadGateway; 
        string title = "Downstream Service Communication Error";
        string typeSuffix = "downstream-service-error";
        string detail = _environment.IsDevelopment() || options.IncludeStackTrace
            ? exception.Message
            : "An error occurred while communicating with a required downstream service. Please try again later.";

        if (httpReqEx?.StatusCode != null)
        {
            statusCode = (int)httpReqEx.StatusCode;
            title = $"Downstream service request failed with status {statusCode}";
            typeSuffix = $"downstream-http-{(int)httpReqEx.StatusCode}"; 
            detail = _environment.IsDevelopment() || options.IncludeStackTrace
                ? $"Downstream service at '{httpReqEx.Source}' responded with {statusCode}. Message: {exception.Message}"
                : $"A downstream service required for this request responded with an error ({statusCode}).";
        }
        else if (exception.InnerException is TimeoutException) 
        {
            statusCode = StatusCodes.Status504GatewayTimeout;
            title = "Downstream Service Timeout";
            typeSuffix = "downstream-timeout";
            detail = "A request to a downstream service timed out.";
        }
        else if (exception.InnerException is System.Net.Sockets.SocketException)
        {
            statusCode = StatusCodes.Status503ServiceUnavailable;
            title = "Downstream Service Unreachable";
            typeSuffix = "downstream-unreachable";
            detail = "A required downstream service is currently unreachable. Please try again later.";
        }


        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, typeSuffix),
            Instance = httpContext.Request.Path
        };

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
