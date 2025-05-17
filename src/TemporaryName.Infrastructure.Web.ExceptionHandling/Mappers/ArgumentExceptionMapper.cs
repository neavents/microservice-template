using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class ArgumentExceptionMapper : IExceptionProblemDetailsMapper
{
    private readonly IHostEnvironment _environment;
    public ArgumentExceptionMapper(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Type HandledExceptionType => typeof(ArgumentException); 
    public int Order => 200; 

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);

        string detailMessage = _environment.IsDevelopment() || options.IncludeStackTrace
            ? exception.Message 
            : "An invalid argument was provided with the request. Please check your input."; 

        string problemTypeSuffix = "invalid-argument";
        string title = "Invalid Argument Provided";

        if (exception is ArgumentNullException argNullException)
        {
            title = "Required Argument Missing";
            problemTypeSuffix = "argument-null";
            detailMessage = _environment.IsDevelopment() || options.IncludeStackTrace
                ? $"Required parameter '{argNullException.ParamName}' was null. {argNullException.Message}"
                : $"A required parameter ('{argNullException.ParamName}') was missing or invalid.";
        }
        else if (exception is ArgumentOutOfRangeException argOutOfRangeException)
        {
            title = "Argument Out Of Range";
            problemTypeSuffix = "argument-out-of-range";
             detailMessage = _environment.IsDevelopment() || options.IncludeStackTrace
                ? $"Parameter '{argOutOfRangeException.ParamName}' was out of range. Actual value: '{argOutOfRangeException.ActualValue}'. {argOutOfRangeException.Message}"
                : $"Parameter '{argOutOfRangeException.ParamName}' was out of the allowed range.";
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title,
            Detail = detailMessage,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, problemTypeSuffix),
            Instance = httpContext.Request.Path
        };

        if (exception is ArgumentException argEx && !string.IsNullOrWhiteSpace(argEx.ParamName))
        {
            problemDetails.Extensions["parameterName"] = argEx.ParamName;
        }
        if (exception is ArgumentOutOfRangeException argOutOfRangeEx && argOutOfRangeEx.ActualValue != null)
        {
             problemDetails.Extensions["actualValue"] = argOutOfRangeEx.ActualValue.ToString();
        }

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
