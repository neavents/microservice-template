using System;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

/// <summary>
/// An <see cref="ITenantStore"/> implementation that retrieves tenant information
/// from a remote HTTP service.
/// </summary>
public partial class RemoteHttpTenantStore : ITenantStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RemoteHttpTenantStore> _logger;
    private readonly MultiTenancyOptions _multiTenancyOptions;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RemoteHttpTenantStore(
        IHttpClientFactory httpClientFactory,
        IOptions<MultiTenancyOptions> multiTenancyOptionsAccessor,
        ILogger<RemoteHttpTenantStore> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
        ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor, nameof(multiTenancyOptionsAccessor));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _multiTenancyOptions = multiTenancyOptionsAccessor.Value;

        if (_multiTenancyOptions is null)
        {
            Error error = new("MultiTenancy.Configuration.OptionsAccessorValueNull.RemoteStore", "IOptions<MultiTenancyOptions>.Value is null. RemoteHttpTenantStore cannot be initialized.");
            LogOptionsAccessorValueNull(_logger, error.Code, error.Description);

            throw new TenantConfigurationException(error.Description!, error);
        }

        if (_multiTenancyOptions.Store.Type != TenantStoreType.RemoteService)
        {
            LogStoreTypeMismatch(_logger, _multiTenancyOptions.Store.Type);
        }

        if (string.IsNullOrWhiteSpace(_multiTenancyOptions.Store.ServiceEndpoint))
        {
            Error error = new("MultiTenancy.Configuration.RemoteStore.MissingServiceEndpoint", $"RemoteHttpTenantStore requires MultiTenancyOptions.Store.ServiceEndpoint to be configured when Store.Type is '{_multiTenancyOptions.Store.Type}'.");
            LogMissingServiceEndpoint(_logger, _multiTenancyOptions.Store.Type, error.Code, error.Description);

            throw new TenantConfigurationException(error.Description!, error);
        }
        if (!Uri.TryCreate(_multiTenancyOptions.Store.ServiceEndpoint, UriKind.Absolute, out _))
        {
            Error error = new("MultiTenancy.Configuration.RemoteStore.InvalidServiceEndpoint", $"MultiTenancyOptions.Store.ServiceEndpoint '{_multiTenancyOptions.Store.ServiceEndpoint}' is not a valid absolute URI.");
            LogInvalidServiceEndpoint(_logger, _multiTenancyOptions.Store.ServiceEndpoint, error.Code, error.Description);
            
            throw new TenantConfigurationException(error.Description!, error);
        }

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        LogInitializationSuccess(_logger, _multiTenancyOptions.Store.ServiceEndpoint);
    }

    public async Task<ITenantInfo?> GetTenantByIdentifierAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            LogGetTenantCalledWithNullOrEmptyId(_logger);
            return null;
        }

        string requestUri = $"{_multiTenancyOptions.Store.ServiceEndpoint!.TrimEnd('/')}/resolve/{Uri.EscapeDataString(id)}";

        try
        {
            HttpClient client = _httpClientFactory.CreateClient("TenantStoreHttpClient");
            LogRequestingTenantInfo(_logger, requestUri, id);

            HttpResponseMessage response = await client.GetAsync(requestUri).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                LogRemoteServiceNotFound(_logger, id, requestUri);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Error error = new("Tenant.Store.Remote.RequestFailed", $"Remote tenant service request failed with status code {response.StatusCode} for identifier '{id}'. URI: {requestUri}. Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}");
                
                LogRemoteRequestFailed(_logger, response.StatusCode, id, requestUri, responseContent, error.Code, error.Description);

                throw new TenantStoreQueryFailedException(error, $"Identifier: {id}, URI: {requestUri}, Status: {response.StatusCode}");
            }

            RemoteTenantDto? tenantDto = await response.Content.ReadFromJsonAsync<RemoteTenantDto>(_jsonSerializerOptions).ConfigureAwait(false);

            if (tenantDto is null || string.IsNullOrWhiteSpace(tenantDto.Id))
            {
                LogRemoteResponseDtoNullOrMissingId(_logger, id, requestUri);

                Error error = new("Tenant.Store.Remote.InvalidData", $"Remote service returned invalid or incomplete tenant data for identifier '{id}'.");
                throw new TenantDeserializationException(error, nameof(RemoteTenantDto));
            }

            Uri? logoUri = null;
            if (!string.IsNullOrWhiteSpace(tenantDto.LogoUrl) &&
                (!Uri.TryCreate(tenantDto.LogoUrl, UriKind.Absolute, out logoUri) ||
                 (logoUri?.Scheme != Uri.UriSchemeHttp && logoUri?.Scheme != Uri.UriSchemeHttps)))
            {
                LogInvalidLogoUrl(_logger, tenantDto.Id, tenantDto.LogoUrl);
                logoUri = null;
            }

            TenantInfo tenantInfo = new(
                id: tenantDto.Id,
                name: tenantDto.Name,
                connectionStringName: tenantDto.ConnectionStringName,
                status: tenantDto.Status,
                domain: tenantDto.Domain,
                subscriptionTier: tenantDto.SubscriptionTier,
                brandingName: tenantDto.BrandingName,
                logoUrl: logoUri,
                dataIsolationMode: tenantDto.DataIsolationMode,
                enabledFeatures: tenantDto.EnabledFeatures,
                customProperties: tenantDto.CustomProperties,
                preferredLocale: tenantDto.PreferredLocale,
                timeZoneId: tenantDto.TimeZoneId,
                dataRegion: tenantDto.DataRegion,
                parentTenantId: tenantDto.ParentTenantId,
                createdAtUtc: tenantDto.CreatedAtUtc == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : tenantDto.CreatedAtUtc, 
                updatedAtUtc: tenantDto.UpdatedAtUtc,
                concurrencyStamp: tenantDto.ConcurrencyStamp
            );

            LogTenantRetrievedSuccessfully(_logger, id, tenantInfo.Id, tenantInfo.Status);
            return tenantInfo;
        }
        catch (TenantConfigurationException) { throw; } 
        catch (HttpRequestException ex) 
        {
            Error error = new("Tenant.Store.Remote.Unavailable", $"Remote tenant service at '{requestUri}' is unavailable or a network error occurred.");
            
            LogRemoteServiceUnavailable(_logger, requestUri, error.Code, error.Description, ex);

            throw new TenantStoreUnavailableException(error, ex, $"Endpoint: {_multiTenancyOptions.Store.ServiceEndpoint}");
        }
        catch (JsonException ex)
        {
            Error error = new("Tenant.Store.Remote.DeserializationFailed", $"Failed to deserialize tenant data from remote service response for identifier '{id}'. URI: {requestUri}");

            LogRemoteDeserializationFailed(_logger, id, requestUri, error.Code, error.Description, ex);
            throw new TenantDeserializationException(error, ex, nameof(RemoteTenantDto));
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) 
        {
            Error error = new("Tenant.Store.Remote.Timeout", $"Request to remote tenant service at '{requestUri}' timed out.");
            LogRemoteRequestTimeout(_logger, requestUri, error.Code, error.Description, ex);

            throw new TenantStoreUnavailableException(error, ex, $"Endpoint: {_multiTenancyOptions.Store.ServiceEndpoint} (Timeout)");
        }
        catch (Exception ex) 
        {
            Error error = new("Tenant.Store.Remote.UnexpectedError", $"An unexpected error occurred in RemoteHttpTenantStore while retrieving tenant by identifier '{id}'. URI: {requestUri}");

            LogRemoteUnexpectedError(_logger, id, requestUri, error.Code, error.Description, ex);
            throw new TenantStoreException(error, ex);
        }
    }
}
