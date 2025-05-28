using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Factories;

public partial class TenantOperationScopeFactory
{
    private const int ClassId = 10;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtCreatingTenantOperation = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtNotFoundOnCreate = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtTenantNotActiveCreationDisallowed = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtTenantNotActive = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantContextSwitched = BaseEventId + (4 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtCreatingTenantOperation,
        Level = LogLevel.Debug,
        Message = "Creating tenant operation scope for {OperationFor}TenantId: {TenantId}")]
    public static partial void LogCreatingTenantOperation(ILogger logger, string tenantId, string operationFor = "");

    [LoggerMessage(
        EventId = EvtNotFoundOnCreate,
        Level = LogLevel.Warning,
        Message = "Tenant with ID: {TenantId} not found when trying to create an operation scope."
    )]
    public static partial void LogNotFoundOnCreate(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = EvtTenantNotActiveCreationDisallowed,
        Level = LogLevel.Error,
        Message = "{ErrorCode}: {ErrorDescription}\nMetadata: {ErrorMetadata}"
    )]
    public static partial void LogTenantNotActiveCreationDisallowed(ILogger logger, string errorCode, string? errorDescription, IReadOnlyDictionary<string, object?>? errorMetadata);

    [LoggerMessage(
        EventId = EvtTenantNotActive,
        Level = LogLevel.Warning,
        Message = "{WarningMessage} Proceeding as {ConditionName} is true."
    )]
    public static partial void LogTenantNotActive(ILogger logger, string warningMessage, string conditionName);

    [LoggerMessage(
        EventId = EvtTenantContextSwitched,
        Level = LogLevel.Information,
        Message = "Tenant context switched. Previous: {PreviousTenantId}, New: {NewTenantId}. Scope: {ScopeDescription}"
    )]
    public static partial void LogTenantContextSwitched(ILogger logger, string? previousTenantId, string newTenantId, string scopeDescription);

    /*
        _logger.LogDebug("Creating tenant operation scope for TenantId: {TenantId}", tenantId);

        Error error = new("Tenant.Scope.NotFoundOnCreate", $"Tenant with ID '{tenantId}' not found when trying to create an operation scope.");
        _logger.LogWarning(error.Description);

        _logger.LogDebug("Creating tenant operation scope for provided ITenantInfo. TenantId: {TenantId}", tenantInfo.Id);
        string logMessage = $"Tenant '{tenantInfoToSet.Id}' is not active (Status: {tenantInfoToSet.Status}) during scope creation.";

        if (!_tenantDataOptions.AllowScopeCreationForNonActiveTenants)
        {
            Error error = Error.Forbidden(
                "Tenant.Scope.NotActiveDisallowed",
                $"{logMessage} Scope creation for non-active tenants is disallowed by configuration.",
                new Dictionary<string, object?> {
                        { "TenantId", tenantInfoToSet.Id },
                        { "TenantStatus", tenantInfoToSet.Status.ToString() }
                }
            );
        _logger.LogError("{ErrorCode}: {ErrorDescription} Metadata: {ErrorMetadata}", error.Code, error.Description, error.Metadata);

        _logger.LogWarning("{WarningMessage} Proceeding as AllowScopeCreationForNonActiveTenants is true.", logMessage);

        _logger.LogInformation("Tenant context switched. Previous: {PreviousTenantId}, New: {NewTenantId}. Scope: {ScopeDescription}", previousTenantInfo?.Id ?? "null", tenantInfoToSet.Id, scopeDescription);

    */
}
