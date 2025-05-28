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

                LogOptionsAccessorValueNull(_logger, error.Code, error.Description);
                throw new TenantConfigurationException(error.Description!, error);
            }

            if (options.Store.Type != TenantStoreType.Configuration)
            {
                LogStoreTypeMismatch(_logger, options.Store.Type);
                _tenantsByIdentifier = new Dictionary<string, ITenantInfo>(StringComparer.OrdinalIgnoreCase); // Empty, but valid state.
                return;
            }

            if (options.Tenants == null)
            {
                Error error = new("MultiTenancy.Configuration.TenantsCollectionNull", $"MultiTenancyOptions.Tenants collection is null, but Store.Type is '{options.Store.Type}'. No tenants can be loaded.");
                LogTenantsCollectionNull(_logger, options.Store.Type, error.Code, error.Description);

                throw new TenantConfigurationException(error.Description!, error);
            }

            if (options.Tenants.Count == 0)
            {
                LogTenantsCollectionEmpty(_logger);
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
                    LogSkippingEntryNullIdentifier(_logger);
                    continue; // Skip this entry, try to load others.
                }
                if (configEntry == null)
                {
                    LogSkippingEntryNullConfig(_logger, identifier);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(configEntry.Id))
                {
                    Error error = new("Tenant.Configuration.EntryMissingId", $"Tenant configuration entry for identifier '{identifier}' is missing the required 'Id' property.");

                    LogEntryMissingRequiredId(_logger, identifier, error.Code, error.Description);
                    throw new TenantConfigurationException(error.Description!, error);
                }

                try
                {
                    Uri? logoUri = null;
                    if (!string.IsNullOrWhiteSpace(configEntry.LogoUrl))
                    {
                        if (!Uri.TryCreate(configEntry.LogoUrl, UriKind.Absolute, out logoUri) ||
                            (logoUri?.Scheme != Uri.UriSchemeHttp && logoUri?.Scheme != Uri.UriSchemeHttps))
                        {
                            LogInvalidLogoUrl(_logger, configEntry.Id, configEntry.LogoUrl);
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
                        LogDuplicateTenantIdentifier(_logger, identifier);
                    }
                }
                catch (ArgumentException ex)
                {
                    Error error = new("Tenant.Configuration.EntryCreationFailed", $"Failed to create TenantInfo object for identifier '{identifier}' from configuration: {ex.Message}");

                    LogTenantInfoCreationArgumentError(_logger, identifier, error.Code, error.Description, ex);
                    throw new TenantConfigurationException(error.Description!, error, ex);
                }
                catch (Exception ex)
                {
                    Error error = new("Tenant.Configuration.UnexpectedEntryError", $"An unexpected error occurred while processing tenant configuration for identifier '{identifier}'.");

                    LogUnexpectedTenantEntryProcessingError(_logger, identifier, error.Code, error.Description, ex);
                    throw new TenantConfigurationException(error.Description!, error, ex);
                }
            }
            _tenantsByIdentifier = tempTenants;
            LogInitializationSuccess(_logger, _tenantsByIdentifier.Count);
        }

        public Task<ITenantInfo?> GetTenantByIdentifierAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                LogGetTenantCalledWithNullOrEmptyId(_logger);

                return Task.FromResult<ITenantInfo?>(null);
            }

            if (_tenantsByIdentifier.TryGetValue(id, out ITenantInfo? tenantInfo))
            { 
                LogTenantFoundByIdentifier(_logger, id, tenantInfo.Id, tenantInfo.Status);

                return Task.FromResult<ITenantInfo?>(tenantInfo);
            }

            LogTenantNotFoundByIdentifier(_logger, id);
            return Task.FromResult<ITenantInfo?>(null);
        }
    }
