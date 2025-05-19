using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

public partial class ConfigurationTenantStore : ITenantStore
    {
        private readonly IReadOnlyDictionary<string, ITenantInfo> _tenantsByIdentifier;
        private readonly ILogger<ConfigurationTenantStore> _logger;

        public ConfigurationTenantStore(IOptionsMonitor<MultiTenancyOptions> multiTenancyOptionsAccessor, ILogger<ConfigurationTenantStore> logger)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            _logger = logger;

            ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor, nameof(multiTenancyOptionsAccessor));

            MultiTenancyOptions options = multiTenancyOptionsAccessor.CurrentValue;
            if (options == null)
            {
                Error error = new("MultiTenancy.Configuration.OptionsAccessorValueNull", "IOptions<MultiTenancyOptions>.Value is null. MultiTenancy configuration is missing or malformed.");
                _logger.LogCritical(error.Description);
                throw new TenantConfigurationException(error.Description, error);
            }

            if (options.Store.Type != TenantStoreType.Configuration)
            {
                _logger.LogInformation("ConfigurationTenantStore initialized, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store will not load tenants from configuration.", options.Store.Type);
                _tenantsByIdentifier = new Dictionary<string, ITenantInfo>(StringComparer.OrdinalIgnoreCase); // Empty, but valid state.
                return;
            }

            if (options.Tenants == null)
            {
                Error error = new("MultiTenancy.Configuration.TenantsCollectionNull", $"MultiTenancyOptions.Tenants collection is null, but Store.Type is '{options.Store.Type}'. No tenants can be loaded.");
                _logger.LogError(error.Description);

                throw new TenantConfigurationException(error.Description, error);
            }

            if (options.Tenants.Count == 0)
            {
                _logger.LogWarning("MultiTenancyOptions.Tenants is empty. ConfigurationTenantStore will be initialized with no tenants.");
                _tenantsByIdentifier = new Dictionary<string, ITenantInfo>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var tempTenants = new Dictionary<string, ITenantInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, TenantConfigurationEntry> entry in options.Tenants)
            {
                string identifier = entry.Key;
                TenantConfigurationEntry configEntry = entry.Value;

                if (string.IsNullOrWhiteSpace(identifier))
                {
                    _logger.LogWarning("Skipping tenant configuration entry: The identifier key is null or whitespace.");
                    continue; // Skip this entry, try to load others.
                }
                if (configEntry == null)
                {
                    _logger.LogWarning("Skipping tenant configuration entry for identifier '{Identifier}': The configuration value is null.", identifier);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(configEntry.Id))
                {
                    Error error = new("Tenant.Configuration.EntryMissingId", $"Tenant configuration entry for identifier '{identifier}' is missing the required 'Id' property.");
                    _logger.LogError(error.Description);
                    throw new TenantConfigurationException(error.Description, error);
                }

                try
                {
                    Uri? logoUri = null;
                    if (!string.IsNullOrWhiteSpace(configEntry.LogoUrl))
                    {
                        if (!Uri.TryCreate(configEntry.LogoUrl, UriKind.Absolute, out logoUri) ||
                            (logoUri?.Scheme != Uri.UriSchemeHttp && logoUri?.Scheme != Uri.UriSchemeHttps))
                        {
                            _logger.LogWarning("Tenant '{TenantId}': Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.", configEntry.Id, configEntry.LogoUrl);
                            logoUri = null;
                        }
                    }

                    TenantInfo tenantInfo = new(
                        id: configEntry.Id,
                        name: configEntry.Name,
                        connectionStringName: configEntry.ConnectionStringName,
                        status: configEntry.Status,
                        domain: configEntry.Domain,
                        subscriptionTier: configEntry.SubscriptionTier,
                        brandingName: configEntry.BrandingName,
                        logoUrl: logoUri,
                        dataIsolationMode: configEntry.DataIsolationMode,
                        enabledFeatures: configEntry.EnabledFeatures, 
                        customProperties: configEntry.CustomProperties, 
                        preferredLocale: configEntry.PreferredLocale,
                        timeZoneId: configEntry.TimeZoneId,
                        dataRegion: configEntry.DataRegion,
                        parentTenantId: configEntry.ParentTenantId
                    );

                    if (!tempTenants.TryAdd(identifier, tenantInfo))
                    {
                         _logger.LogWarning("Duplicate tenant identifier '{Identifier}' encountered in configuration. The first valid entry for this identifier was used. Subsequent entries are ignored.", identifier);
                    }
                }
                catch (ArgumentException ex) // From TenantInfo constructor (e.g., if Id was somehow invalid despite prior check)
                {
                    Error error = new("Tenant.Configuration.EntryCreationFailed", $"Failed to create TenantInfo object for identifier '{identifier}' from configuration: {ex.Message}");
                    _logger.LogError(ex, error.Description);
                    throw new TenantConfigurationException(error.Description, error, ex);
                }
                catch (Exception ex) // Catch-all for unexpected issues during mapping
                {
                    Error error = new("Tenant.Configuration.UnexpectedEntryError", $"An unexpected error occurred while processing tenant configuration for identifier '{identifier}'.");
                    _logger.LogCritical(ex, error.Description); // Critical because it's an unknown processing error.
                    throw new TenantConfigurationException(error.Description, error, ex);
                }
            }
            _tenantsByIdentifier = tempTenants;
            _logger.LogInformation("ConfigurationTenantStore initialized successfully with {TenantCount} tenants from configuration.", _tenantsByIdentifier.Count);
        }

        public Task<ITenantInfo?> GetTenantByIdentifierAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                // This is an invalid argument for lookup, not necessarily an "exception" unless the caller expects one.
                // Returning null is standard for "not found" or "invalid input for search".
                _logger.LogDebug("GetTenantByIdentifierAsync called with null or empty identifier. Returning null.");
                return Task.FromResult<ITenantInfo?>(null);
            }

            if (_tenantsByIdentifier.TryGetValue(id, out ITenantInfo? tenantInfo))
            {
                //ArgumentNullException.ThrowIfNull(tenantInfo);

                _logger.LogDebug("Tenant found for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.", id, tenantInfo.Id, tenantInfo.Status);
                return Task.FromResult(tenantInfo); // Returns the tenant as is, status check is up to caller.
            }

            _logger.LogDebug("No tenant found in ConfigurationTenantStore for identifier '{Identifier}'.", id);
            return Task.FromResult<ITenantInfo?>(null);
        }
    }
