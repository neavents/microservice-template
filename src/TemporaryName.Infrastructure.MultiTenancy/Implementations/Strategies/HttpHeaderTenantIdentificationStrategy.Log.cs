using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class HttpHeaderTenantIdentificationStrategy
{
    private const int ClassId = 65;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtMissingHeaderNameParameter = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtHttpContextRequestNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtHttpContextRequestHeadersNull = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantIdentifiedFromHeader = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtHeaderFoundButValueNullOrWhitespace = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtHeaderFoundButEmpty = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtHeaderNotFound = BaseEventId + (7 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtMissingHeaderNameParameter,
        Level = LogLevel.Critical,
        Message = "HttpHeaderTenantIdentificationStrategy requires ParameterName (the HTTP header name) to be configured. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMissingHeaderNameParameter(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "HttpHeaderTenantIdentificationStrategy initialized. Will look for tenant identifier in HTTP header: '{HeaderName}'.")]
    public static partial void LogInitializationSuccess(ILogger logger, string headerName);

    [LoggerMessage(
        EventId = EvtHttpContextRequestNull,
        Level = LogLevel.Warning,
        Message = "HttpContext.Request is null. Cannot identify tenant using HttpHeader strategy.")]
    public static partial void LogHttpContextRequestNull(ILogger logger);

    [LoggerMessage(
        EventId = EvtHttpContextRequestHeadersNull,
        Level = LogLevel.Warning,
        Message = "HttpContext.Request.Headers is null. Cannot identify tenant using HttpHeader strategy.")]
    public static partial void LogHttpContextRequestHeadersNull(ILogger logger);

    [LoggerMessage(
        EventId = EvtTenantIdentifiedFromHeader,
        Level = LogLevel.Debug,
        Message = "HttpHeaderTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from HTTP header '{HeaderName}'.")]
    public static partial void LogTenantIdentifiedFromHeader(ILogger logger, string? tenantIdentifier, string headerName);

    [LoggerMessage(
        EventId = EvtHeaderFoundButValueNullOrWhitespace,
        Level = LogLevel.Debug,
        Message = "HttpHeaderTenantIdentificationStrategy: HTTP header '{HeaderName}' found, but its value(s) are null or whitespace.")]
    public static partial void LogHeaderFoundButValueNullOrWhitespace(ILogger logger, string headerName);

    [LoggerMessage(
        EventId = EvtHeaderFoundButEmpty,
        Level = LogLevel.Debug,
        Message = "HttpHeaderTenantIdentificationStrategy: HTTP header '{HeaderName}' found, but it is empty.")]
    public static partial void LogHeaderFoundButEmpty(ILogger logger, string headerName);

    [LoggerMessage(
        EventId = EvtHeaderNotFound,
        Level = LogLevel.Debug,
        Message = "HttpHeaderTenantIdentificationStrategy: HTTP header '{HeaderName}' not found in the request.")]
    public static partial void LogHeaderNotFound(ILogger logger, string headerName);
}
