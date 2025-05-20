using System;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

public partial class InMemoryTenantStore
{
    private const int ClassId = 45;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtSkippingNullTenantOnInit = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtDuplicateTenantIdOnInit = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtGetTenantCalledWithNullOrEmptyId = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtTenantFoundByIdentifier = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtTenantNotFoundByIdentifier = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtTenantAddedOrUpdated = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtTenantRemoved = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtRemoveTenantNotFound = BaseEventId + (8 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtSkippingNullTenantOnInit,
        Level = LogLevel.Warning,
        Message = "Skipping null tenant or tenant with null/empty ID during InMemoryTenantStore initialization.")]
    public static partial void LogSkippingNullTenantOnInit(ILogger logger);

    [LoggerMessage(
        EventId = EvtDuplicateTenantIdOnInit,
        Level = LogLevel.Warning,
        Message = "Duplicate tenant ID '{TenantId}' encountered during InMemoryTenantStore initialization. The first entry was kept.")]
    public static partial void LogDuplicateTenantIdOnInit(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "InMemoryTenantStore initialized with {TenantCount} tenants.")]
    public static partial void LogInitializationSuccess(ILogger logger, int tenantCount);

    [LoggerMessage(
        EventId = EvtGetTenantCalledWithNullOrEmptyId,
        Level = LogLevel.Debug,
        Message = "InMemoryTenantStore.GetTenantByIdentifierAsync called with null or empty identifier.")]
    public static partial void LogGetTenantCalledWithNullOrEmptyId(ILogger logger);

    [LoggerMessage(
        EventId = EvtTenantFoundByIdentifier,
        Level = LogLevel.Debug,
        Message = "Tenant found in InMemoryTenantStore for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.")]
    public static partial void LogTenantFoundByIdentifier(ILogger logger, string identifier, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtTenantNotFoundByIdentifier,
        Level = LogLevel.Debug,
        Message = "No tenant found in InMemoryTenantStore for identifier '{Identifier}'.")]
    public static partial void LogTenantNotFoundByIdentifier(ILogger logger, string identifier);

    [LoggerMessage(
        EventId = EvtTenantAddedOrUpdated,
        Level = LogLevel.Information,
        Message = "Tenant with lookup identifier '{IdentifierForLookup}' (Tenant ID: '{TenantId}') was added/updated in InMemoryTenantStore.")]
    public static partial void LogTenantAddedOrUpdated(ILogger logger, string identifierForLookup, string tenantId);

    [LoggerMessage(
        EventId = EvtTenantRemoved,
        Level = LogLevel.Information,
        Message = "Tenant with lookup identifier '{IdentifierForLookup}' (Tenant ID: '{TenantId}') was removed from InMemoryTenantStore.")]
    public static partial void LogTenantRemoved(ILogger logger, string identifierForLookup, string? tenantId); // tenantId can be null if removedTenant was null

    [LoggerMessage(
        EventId = EvtRemoveTenantNotFound,
        Level = LogLevel.Warning,
        Message = "Attempted to remove tenant with lookup identifier '{IdentifierForLookup}', but it was not found in InMemoryTenantStore.")]
    public static partial void LogRemoveTenantNotFound(ILogger logger, string identifierForLookup);
}
