using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TemporaryName.Domain.Exceptions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Abstractions;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Constants;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Helpers;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Services;
using TemporaryName.Infrastructure.Web.ExceptionHandling.Settings;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling.Mappers;

public class BusinessRuleViolationDomainExceptionMapper : IExceptionProblemDetailsMapper
{
    public Type HandledExceptionType => typeof(BusinessRuleViolationDomainException);
    public int Order => 92; // Higher precedence than generic DomainExceptionMapper

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception,
        GlobalExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exception);
        
        if (exception is not BusinessRuleViolationDomainException businessRuleException)
        {
            throw new InvalidOperationException($"Mapper {nameof(BusinessRuleViolationDomainExceptionMapper)} received an exception of type {exception.GetType().FullName} which it cannot handle.");
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = businessRuleException.ErrorDetails.Code ?? "Business Rule Violation",
            Detail = businessRuleException.ErrorDetails.Description ?? businessRuleException.Message,
            Type = ProblemDetailsHelpers.CombineProblemTypeUri(options.ProblemTypeUriBase, 
                !string.IsNullOrWhiteSpace(businessRuleException.ErrorDetails.Code) 
                ? businessRuleException.ErrorDetails.Code.ToLowerInvariant().Replace(".", "-") 
                : "business-rule-violation"),
            Instance = httpContext.Request.Path
        };

        if (businessRuleException.ErrorDetails.Metadata != null && businessRuleException.ErrorDetails.Metadata.Any())
        {
            problemDetails.Extensions[ProblemDetailsConstants.ErrorMetadataExtensionKey] = businessRuleException.ErrorDetails.Metadata;
        }
        
        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions[ProblemDetailsConstants.StackTraceExtensionKey] = MapperHelpers.GetSanitizedStackTrace(exception);
        }
        return problemDetails;
    }
}
