using System;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

public partial class RemoteHttpTenantStore
{
    private const int ClassId = 50;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);
    public const int EvtOptionsAccessorValueNull = BaseEventId + (0 * Logging.IncrementPerLog);
    public const int EvtStoreTypeMismatch = BaseEventId + (1 * Logging.IncrementPerLog);
    public const int EvtMissingServiceEndpoint = BaseEventId + (2 * Logging.IncrementPerLog);
    public const int EvtInvalidServiceEndpoint = BaseEventId + (3 * Logging.IncrementPerLog);
    public const int EvtInitializationSuccess = BaseEventId + (4 * Logging.IncrementPerLog);
    public const int EvtGetTenantCalledWithNullOrEmptyId = BaseEventId + (5 * Logging.IncrementPerLog);
    public const int EvtRequestingTenantInfo = BaseEventId + (6 * Logging.IncrementPerLog);
    public const int EvtRemoteServiceNotFound = BaseEventId + (7 * Logging.IncrementPerLog);
    public const int EvtRemoteRequestFailed = BaseEventId + (8 * Logging.IncrementPerLog);
    public const int EvtRemoteResponseDtoNullOrMissingId = BaseEventId + (9 * Logging.IncrementPerLog);
    public const int EvtInvalidLogoUrl = BaseEventId + (10 * Logging.IncrementPerLog);
    public const int EvtTenantRetrievedSuccessfully = BaseEventId + (11 * Logging.IncrementPerLog);
    public const int EvtRemoteServiceUnavailable = BaseEventId + (12 * Logging.IncrementPerLog);
    public const int EvtRemoteDeserializationFailed = BaseEventId + (13 * Logging.IncrementPerLog);
    public const int EvtRemoteRequestTimeout = BaseEventId + (14 * Logging.IncrementPerLog);
    public const int EvtRemoteUnexpectedError = BaseEventId + (15 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtOptionsAccessorValueNull,
        Level = LogLevel.Critical,
        Message = "IOptions<MultiTenancyOptions>.Value is null. RemoteHttpTenantStore cannot be initialized. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogOptionsAccessorValueNull(ILogger logger, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtStoreTypeMismatch,
        Level = LogLevel.Warning,
        Message = "RemoteHttpTenantStore is registered, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store might not be used as intended by the configuration.")]
    public static partial void LogStoreTypeMismatch(ILogger logger, TenantStoreType storeType);

    [LoggerMessage(
        EventId = EvtMissingServiceEndpoint,
        Level = LogLevel.Critical,
        Message = "RemoteHttpTenantStore requires MultiTenancyOptions.Store.ServiceEndpoint to be configured when Store.Type is '{StoreType}'. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogMissingServiceEndpoint(ILogger logger, TenantStoreType storeType, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtInvalidServiceEndpoint,
        Level = LogLevel.Critical,
        Message = "MultiTenancyOptions.Store.ServiceEndpoint '{ServiceEndpoint}' is not a valid absolute URI. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogInvalidServiceEndpoint(ILogger logger, string? serviceEndpoint, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtInitializationSuccess,
        Level = LogLevel.Information,
        Message = "RemoteHttpTenantStore initialized. Will use service endpoint: '{ServiceEndpoint}'.")]
    public static partial void LogInitializationSuccess(ILogger logger, string? serviceEndpoint);

    [LoggerMessage(
        EventId = EvtGetTenantCalledWithNullOrEmptyId,
        Level = LogLevel.Debug,
        Message = "RemoteHttpTenantStore.GetTenantByIdentifierAsync called with null or empty identifier.")]
    public static partial void LogGetTenantCalledWithNullOrEmptyId(ILogger logger);

    [LoggerMessage(
        EventId = EvtRequestingTenantInfo,
        Level = LogLevel.Debug,
        Message = "RemoteHttpTenantStore: Requesting tenant info from '{RequestUri}' for identifier '{Identifier}'.")]
    public static partial void LogRequestingTenantInfo(ILogger logger, string requestUri, string identifier);

    [LoggerMessage(
        EventId = EvtRemoteServiceNotFound,
        Level = LogLevel.Debug,
        Message = "Remote service returned 404 Not Found for tenant identifier '{Identifier}' at '{RequestUri}'.")]
    public static partial void LogRemoteServiceNotFound(ILogger logger, string identifier, string requestUri);

    [LoggerMessage(
        EventId = EvtRemoteRequestFailed,
        Level = LogLevel.Error, // Matched original LogError
        Message = "Remote tenant service request failed with status code {StatusCode} for identifier '{Identifier}'. URI: {RequestUri}. Response: {ResponseContent}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogRemoteRequestFailed(ILogger logger, HttpStatusCode statusCode, string identifier, string requestUri, string responseContent, string errorCode, string? errorDescription);

    [LoggerMessage(
        EventId = EvtRemoteResponseDtoNullOrMissingId,
        Level = LogLevel.Warning,
        Message = "Remote tenant service returned a successful response for identifier '{Identifier}', but the DTO is null or has a missing ID. URI: {RequestUri}")]
    public static partial void LogRemoteResponseDtoNullOrMissingId(ILogger logger, string identifier, string requestUri);

    [LoggerMessage(
        EventId = EvtInvalidLogoUrl,
        Level = LogLevel.Warning,
        Message = "Tenant '{TenantId}' from Remote: Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.")]
    public static partial void LogInvalidLogoUrl(ILogger logger, string tenantId, string? logoUrl);

    [LoggerMessage(
        EventId = EvtTenantRetrievedSuccessfully,
        Level = LogLevel.Debug,
        Message = "Tenant successfully retrieved from remote service for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.")]
    public static partial void LogTenantRetrievedSuccessfully(ILogger logger, string identifier, string tenantId, TenantStatus tenantStatus);

    [LoggerMessage(
        EventId = EvtRemoteServiceUnavailable,
        Level = LogLevel.Error,
        Message = "Remote tenant service at '{RequestUri}' is unavailable or a network error occurred. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogRemoteServiceUnavailable(ILogger logger, string requestUri, string errorCode, string? errorDescription, HttpRequestException ex);

    [LoggerMessage(
        EventId = EvtRemoteDeserializationFailed,
        Level = LogLevel.Error,
        Message = "Failed to deserialize tenant data from remote service response for identifier '{Identifier}'. URI: {RequestUri}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogRemoteDeserializationFailed(ILogger logger, string identifier, string requestUri, string errorCode, string? errorDescription, JsonException ex);

    [LoggerMessage(
        EventId = EvtRemoteRequestTimeout,
        Level = LogLevel.Error,
        Message = "Request to remote tenant service at '{RequestUri}' timed out. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogRemoteRequestTimeout(ILogger logger, string requestUri, string errorCode, string? errorDescription, TaskCanceledException ex);

    [LoggerMessage(
        EventId = EvtRemoteUnexpectedError,
        Level = LogLevel.Error,
        Message = "An unexpected error occurred in RemoteHttpTenantStore while retrieving tenant by identifier '{Identifier}'. URI: {RequestUri}. Error Code: {ErrorCode}, Details: {ErrorDescription}")]
    public static partial void LogRemoteUnexpectedError(ILogger logger, string identifier, string requestUri, string errorCode, string? errorDescription, Exception ex);
}
