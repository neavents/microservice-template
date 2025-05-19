using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations;

public partial class TenantContext
{
    private const int ClassId = 80;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);

    // EventId Definitions
    public const int EvtOverwritingTenantContext = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtClearingTenantContext = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtCurrentTenantSet = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtTenantResolvedButNotActive = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantSetToNull = BaseEventId + (4 * Logging.IncrementPerLog);

    // LoggerMessage Definitions

    [LoggerMessage(
        EventId = EvtOverwritingTenantContext,
        Level = LogLevel.Warning,
        Message = "TenantContext: Overwriting an existing tenant context. Previous Tenant ID: '{PreviousTenantId}', New Tenant ID: '{NewTenantId}'. This might be expected during re-evaluation or indicate an issue.")]
    public static partial void LogOverwritingTenantContext(ILogger logger, string previousTenantId, string newTenantId);

    [LoggerMessage(
        EventId = EvtClearingTenantContext,
        Level = LogLevel.Information,
        Message = "TenantContext: Clearing previously set tenant context. Previous Tenant ID: '{PreviousTenantId}'.")]
    public static partial void LogClearingTenantContext(ILogger logger, string previousTenantId);

    [LoggerMessage(
        EventId = EvtCurrentTenantSet,
        Level = LogLevel.Debug,
        Message = "TenantContext: Current tenant set. Tenant ID: '{TenantId}', Status: '{TenantStatus}'. IsActiveAndResolved: {IsActiveAndResolved}")]
    public static partial void LogCurrentTenantSet(ILogger logger, string tenantId, TenantStatus tenantStatus, bool isActiveAndResolved);

    [LoggerMessage(
        EventId = EvtTenantResolvedButNotActive,
        Level = LogLevel.Information,
        Message = "TenantContext: Tenant '{TenantId}' is resolved but its status is '{TenantStatus}'. Access control should be based on this status.")]
    public static partial void LogTenantResolvedButNotActive(ILogger logger, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtTenantSetToNull,
        Level = LogLevel.Debug,
        Message = "TenantContext: Current tenant explicitly set to null.")]
    public static partial void LogTenantSetToNull(ILogger logger);
}
