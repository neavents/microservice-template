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

        if (_multiTenancyOptions == null)
        {
            Error error = new("MultiTenancy.Configuration.OptionsAccessorValueNull.RemoteStore", "IOptions<MultiTenancyOptions>.Value is null. RemoteHttpTenantStore cannot be initialized.");
            _logger.LogCritical(error.Description);
            throw new TenantConfigurationException(error.Description, error);
        }

        if (_multiTenancyOptions.Store.Type != TenantStoreType.RemoteService)
        {
            _logger.LogWarning("RemoteHttpTenantStore is registered, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store might not be used as intended by the configuration.", _multiTenancyOptions.Store.Type);
        }

        if (string.IsNullOrWhiteSpace(_multiTenancyOptions.Store.ServiceEndpoint))
        {
            Error error = new("MultiTenancy.Configuration.RemoteStore.MissingServiceEndpoint", $"RemoteHttpTenantStore requires MultiTenancyOptions.Store.ServiceEndpoint to be configured when Store.Type is '{_multiTenancyOptions.Store.Type}'.");
            _logger.LogCritical(error.Description);
            throw new TenantConfigurationException(error.Description, error);
        }
        if (!Uri.TryCreate(_multiTenancyOptions.Store.ServiceEndpoint, UriKind.Absolute, out _))
        {
            Error error = new("MultiTenancy.Configuration.RemoteStore.InvalidServiceEndpoint", $"MultiTenancyOptions.Store.ServiceEndpoint '{_multiTenancyOptions.Store.ServiceEndpoint}' is not a valid absolute URI.");
            _logger.LogCritical(error.Description);
            throw new TenantConfigurationException(error.Description, error);
        }

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        _logger.LogInformation("RemoteHttpTenantStore initialized. Will use service endpoint: '{ServiceEndpoint}'.", _multiTenancyOptions.Store.ServiceEndpoint);
    }

    public async Task<ITenantInfo?> GetTenantByIdentifierAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            _logger.LogDebug("RemoteHttpTenantStore.GetTenantByIdentifierAsync called with null or empty identifier.");
            return null;
        }

        string requestUri = $"{_multiTenancyOptions.Store.ServiceEndpoint!.TrimEnd('/')}/resolve/{Uri.EscapeDataString(identifier)}";

        try
        {
            HttpClient client = _httpClientFactory.CreateClient("TenantStoreHttpClient");
            _logger.LogDebug("RemoteHttpTenantStore: Requesting tenant info from '{RequestUri}' for identifier '{Identifier}'.", requestUri, identifier);

            HttpResponseMessage response = await client.GetAsync(requestUri);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Remote service returned 404 Not Found for tenant identifier '{Identifier}' at '{RequestUri}'.", identifier, requestUri);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Error error = new("Tenant.Store.Remote.RequestFailed", $"Remote tenant service request failed with status code {response.StatusCode} for identifier '{identifier}'. URI: {requestUri}. Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}");
                _logger.LogError(error.Description);

                throw new TenantStoreQueryFailedException(error, $"Identifier: {identifier}, URI: {requestUri}, Status: {response.StatusCode}");
            }

            RemoteTenantDto? tenantDto = await response.Content.ReadFromJsonAsync<RemoteTenantDto>(_jsonSerializerOptions).ConfigureAwait(false);

            if (tenantDto == null || string.IsNullOrWhiteSpace(tenantDto.Id))
            {
                _logger.LogWarning("Remote tenant service returned a successful response for identifier '{Identifier}', but the DTO is null or has a missing ID. URI: {RequestUri}", identifier, requestUri);
                // This could be a TenantDeserializationException or a specific "InvalidRemoteTenantDataException".
                Error error = new("Tenant.Store.Remote.InvalidData", $"Remote service returned invalid or incomplete tenant data for identifier '{identifier}'.");
                throw new TenantDeserializationException(error, nameof(RemoteTenantDto));
            }

            Uri? logoUri = null;
            if (!string.IsNullOrWhiteSpace(tenantDto.LogoUrl) &&
                (!Uri.TryCreate(tenantDto.LogoUrl, UriKind.Absolute, out logoUri) ||
                 (logoUri?.Scheme != Uri.UriSchemeHttp && logoUri?.Scheme != Uri.UriSchemeHttps)))
            {
                _logger.LogWarning("Tenant '{TenantId}' from Remote: Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.", tenantDto.Id, tenantDto.LogoUrl);
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

            _logger.LogDebug("Tenant successfully retrieved from remote service for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.", identifier, tenantInfo.Id, tenantInfo.Status);
            return tenantInfo;
        }
        catch (TenantConfigurationException) { throw; } 
        catch (HttpRequestException ex) 
        {
            Error error = new("Tenant.Store.Remote.Unavailable", $"Remote tenant service at '{requestUri}' is unavailable or a network error occurred.");
            _logger.LogError(ex, error.Description);
            throw new TenantStoreUnavailableException(error, ex, $"Endpoint: {_multiTenancyOptions.Store.ServiceEndpoint}");
        }
        catch (JsonException ex)
        {
            Error error = new("Tenant.Store.Remote.DeserializationFailed", $"Failed to deserialize tenant data from remote service response for identifier '{identifier}'. URI: {requestUri}");
            _logger.LogError(ex, error.Description);
            throw new TenantDeserializationException(error, ex, nameof(RemoteTenantDto));
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) 
        {
            Error error = new("Tenant.Store.Remote.Timeout", $"Request to remote tenant service at '{requestUri}' timed out.");
            _logger.LogError(ex, error.Description);
            throw new TenantStoreUnavailableException(error, ex, $"Endpoint: {_multiTenancyOptions.Store.ServiceEndpoint} (Timeout)");
        }
        catch (Exception ex) 
        {
            Error error = new("Tenant.Store.Remote.UnexpectedError", $"An unexpected error occurred in RemoteHttpTenantStore while retrieving tenant by identifier '{identifier}'. URI: {requestUri}");
            _logger.LogError(ex, error.Description);
            throw new TenantStoreException(error, ex);
        }
    }
}
