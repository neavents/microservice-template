using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class RouteValueTenantIdentificationStrategy
{
    private const int ClassId = 75;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);

    // EventId Definitions
    public const int EvtMissingRouteValueKeyParameter = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtRouteDataNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtRouteValueFoundButIsNull = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantIdentifiedFromRouteValue = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtRouteValueStringNullOrWhitespace = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtRouteValueKeyNotFound = BaseEventId + (6 * Logging.IncrementPerLog);

    // LoggerMessage Definitions

    [LoggerMessage(
        EventId = EvtMissingRouteValueKeyParameter,
        Level = LogLevel.Critical,
        Message = "RouteValueTenantIdentificationStrategy requires ParameterName (the route value key) to be configured. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMissingRouteValueKeyParameter(ILogger logger, string errorCode, string errorDescription);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "RouteValueTenantIdentificationStrategy initialized. Will look for tenant identifier in route value key: '{RouteValueKey}'.")]
    public static partial void LogInitializationSuccess(ILogger logger, string routeValueKey);

    [LoggerMessage(
        EventId = EvtRouteDataNull,
        Level = LogLevel.Debug,
        Message = "RouteValueTenantIdentificationStrategy: RouteData is null for the current request. This strategy requires routing to be active.")]
    public static partial void LogRouteDataNull(ILogger logger);

    [LoggerMessage(
        EventId = EvtRouteValueFoundButIsNull,
        Level = LogLevel.Debug,
        Message = "RouteValueTenantIdentificationStrategy: Route value key '{RouteValueKey}' found, but its value is null.")]
    public static partial void LogRouteValueFoundButIsNull(ILogger logger, string routeValueKey);

    [LoggerMessage(
        EventId = EvtTenantIdentifiedFromRouteValue,
        Level = LogLevel.Debug,
        Message = "RouteValueTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from route value key '{RouteValueKey}'.")]
    public static partial void LogTenantIdentifiedFromRouteValue(ILogger logger, string? tenantIdentifier, string routeValueKey);

    [LoggerMessage(
        EventId = EvtRouteValueStringNullOrWhitespace,
        Level = LogLevel.Debug,
        Message = "RouteValueTenantIdentificationStrategy: Route value key '{RouteValueKey}' found, but its string representation is null or whitespace. Original value: '{OriginalValue}'.")]
    public static partial void LogRouteValueStringNullOrWhitespace(ILogger logger, string routeValueKey, object originalValue);

    [LoggerMessage(
        EventId = EvtRouteValueKeyNotFound,
        Level = LogLevel.Debug,
        Message = "RouteValueTenantIdentificationStrategy: Route value key '{RouteValueKey}' not found in RouteData.Values.")]
    public static partial void LogRouteValueKeyNotFound(ILogger logger, string routeValueKey);
}
