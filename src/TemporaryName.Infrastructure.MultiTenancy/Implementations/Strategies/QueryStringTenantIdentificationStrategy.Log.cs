using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class QueryStringTenantIdentificationStrategy
{
    private const int ClassId = 70;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);

    // EventId Definitions
    public const int EvtMissingQueryParameterName = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtHttpContextRequestNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtTenantIdentifiedFromQuery = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtQueryParamFoundButValueNullOrWhitespace = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtQueryParamFoundButEmpty = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtQueryParamNotFound = BaseEventId + (6 * Logging.IncrementPerLog);

    // LoggerMessage Definitions

    [LoggerMessage(
        EventId = EvtMissingQueryParameterName,
        Level = LogLevel.Critical,
        Message = "QueryStringTenantIdentificationStrategy requires ParameterName (the query string key) to be configured. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMissingQueryParameterName(ILogger logger, string errorCode, string errorDescription);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "QueryStringTenantIdentificationStrategy initialized. Will look for tenant identifier in query parameter: '{QueryParameterName}'.")]
    public static partial void LogInitializationSuccess(ILogger logger, string queryParameterName);

    [LoggerMessage(
        EventId = EvtHttpContextRequestNull,
        Level = LogLevel.Warning,
        Message = "HttpContext.Request is null. Cannot identify tenant using QueryString strategy.")]
    public static partial void LogHttpContextRequestNull(ILogger logger);

    [LoggerMessage(
        EventId = EvtTenantIdentifiedFromQuery,
        Level = LogLevel.Debug,
        Message = "QueryStringTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from query string parameter '{QueryParameterName}'.")]
    public static partial void LogTenantIdentifiedFromQuery(ILogger logger, string? tenantIdentifier, string queryParameterName);

    [LoggerMessage(
        EventId = EvtQueryParamFoundButValueNullOrWhitespace,
        Level = LogLevel.Debug,
        Message = "QueryStringTenantIdentificationStrategy: Query string parameter '{QueryParameterName}' found, but its value(s) are null or whitespace.")]
    public static partial void LogQueryParamFoundButValueNullOrWhitespace(ILogger logger, string queryParameterName);

    [LoggerMessage(
        EventId = EvtQueryParamFoundButEmpty,
        Level = LogLevel.Debug,
        Message = "QueryStringTenantIdentificationStrategy: Query string parameter '{QueryParameterName}' found, but it is empty.")]
    public static partial void LogQueryParamFoundButEmpty(ILogger logger, string queryParameterName);

    [LoggerMessage(
        EventId = EvtQueryParamNotFound,
        Level = LogLevel.Debug,
        Message = "QueryStringTenantIdentificationStrategy: Query string parameter '{QueryParameterName}' not found in the request.")]
    public static partial void LogQueryParamNotFound(ILogger logger, string queryParameterName);
}
