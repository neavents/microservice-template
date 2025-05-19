using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Strategies;

public partial class HostHeaderTenantIdentificationStrategy
{
    private const int ClassId = 60;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);

    // EventId Definitions
    public const int EvtParameterNameProvidedButUnused = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtHttpContextRequestNull = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtHostHeaderMissingOrEmpty = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtHostIdentifierEmptyAfterSplit = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtTenantIdentifiedFromHost = BaseEventId + (5 * Logging.IncrementPerLog);

    // LoggerMessage Definitions

    [LoggerMessage(
        EventId = EvtParameterNameProvidedButUnused,
        Level = LogLevel.Information,
        Message = "HostHeaderTenantIdentificationStrategy: ParameterName '{ParameterName}' was provided in options. This strategy currently does not use it but could be extended.")]
    public static partial void LogParameterNameProvidedButUnused(ILogger logger, string? parameterName);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "HostHeaderTenantIdentificationStrategy initialized.")]
    public static partial void LogInitializationSuccess(ILogger logger);

    [LoggerMessage(
        EventId = EvtHttpContextRequestNull,
        Level = LogLevel.Warning,
        Message = "HttpContext.Request is null. Cannot identify tenant using HostHeader strategy.")]
    public static partial void LogHttpContextRequestNull(ILogger logger);

    [LoggerMessage(
        EventId = EvtHostHeaderMissingOrEmpty,
        Level = LogLevel.Debug,
        Message = "Host header is missing, empty, or has no value. Cannot identify tenant using HostHeader strategy.")]
    public static partial void LogHostHeaderMissingOrEmpty(ILogger logger);

    [LoggerMessage(
        EventId = EvtHostIdentifierEmptyAfterSplit,
        Level = LogLevel.Debug,
        Message = "After splitting port, the host identifier part is empty for host '{FullHost}'.")]
    public static partial void LogHostIdentifierEmptyAfterSplit(ILogger logger, string fullHost);

    [LoggerMessage(
        EventId = EvtTenantIdentifiedFromHost,
        Level = LogLevel.Debug,
        Message = "HostHeaderTenantIdentificationStrategy: Identified potential tenant identifier '{TenantIdentifier}' from host '{FullHost}'.")]
    public static partial void LogTenantIdentifiedFromHost(ILogger logger, string tenantIdentifier, string fullHost);
}
