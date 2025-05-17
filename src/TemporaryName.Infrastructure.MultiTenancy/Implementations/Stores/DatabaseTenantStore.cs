using System;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.DataAccess.Abstractions;
using TemporaryName.Infrastructure.DataAccess.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;

public class DatabaseTenantStore : ITenantStore
    {
        private readonly IDbConnectionFactory _dbConnectionFactory; // CHANGED: Use the new abstraction
        private readonly ILogger<DatabaseTenantStore> _logger;
        private readonly MultiTenancyOptions _multiTenancyOptions;

        // TenantDbo class remains the same...
        private class TenantDbo
        {
            public string Id { get; set; } = string.Empty;
            public string? Name { get; set; }
            public string? ConnectionStringName { get; set; } // This is the tenant-specific CS, not the metadata one
            public int Status { get; set; }
            public string? Domain { get; set; }
            public string? SubscriptionTier { get; set; }
            public string? BrandingName { get; set; }
            public string? LogoUrl { get; set; }
            public int DataIsolationMode { get; set; }
            public string? EnabledFeaturesJson { get; set; }
            public string? CustomPropertiesJson { get; set; }
            public string? PreferredLocale { get; set; }
            public string? TimeZoneId { get; set; }
            public string? DataRegion { get; set; }
            public string? ParentTenantId { get; set; }
            public DateTimeOffset CreatedAtUtc { get; set; }
            public DateTimeOffset? UpdatedAtUtc { get; set; }
            public string? ConcurrencyStamp { get; set; }
            public string LookupIdentifier { get; set; } = string.Empty; // Key for finding this tenant record
        }


        public DatabaseTenantStore(
            IDbConnectionFactory dbConnectionFactory, // CHANGED: Injected dependency
            IOptions<MultiTenancyOptions> multiTenancyOptionsAccessor,
            ILogger<DatabaseTenantStore> logger)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory, nameof(dbConnectionFactory));
            ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor, nameof(multiTenancyOptionsAccessor));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
            _multiTenancyOptions = multiTenancyOptionsAccessor.Value;

            if (_multiTenancyOptions == null) // This check is fine
            {
                Error error = new("MultiTenancy.Configuration.OptionsAccessorValueNull.DbStore", "IOptions<MultiTenancyOptions>.Value is null. DatabaseTenantStore cannot be initialized.");
                _logger.LogCritical(error.Description);
                throw new TenantConfigurationException(error.Description, error);
            }

            // This check is still relevant for configuration sanity.
            if (_multiTenancyOptions.Store.Type != TenantStoreType.Database)
            {
                _logger.LogWarning("DatabaseTenantStore is registered, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store might not be used as intended by the configuration.", _multiTenancyOptions.Store.Type);
            }

            // This connection string name is for the database *containing tenant metadata*.
            if (string.IsNullOrWhiteSpace(_multiTenancyOptions.Store.ConnectionStringName))
            {
                Error error = new("MultiTenancy.Configuration.DbStore.MissingConnectionStringName", $"DatabaseTenantStore requires MultiTenancyOptions.Store.ConnectionStringName to be configured for the tenant metadata database.");
                _logger.LogCritical(error.Description);
                throw new TenantConfigurationException(error.Description, error);
            }
            _logger.LogInformation("DatabaseTenantStore initialized. Will use connection string name '{ConnectionStringName}' for tenant metadata via IDbConnectionFactory.", _multiTenancyOptions.Store.ConnectionStringName);
        }

        public async Task<ITenantInfo?> GetTenantByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                _logger.LogDebug("DatabaseTenantStore.GetTenantByIdentifierAsync called with null or empty identifier.");
                return null;
            }

            // SQL query (ensure this is compatible with your chosen DB provider for tenant metadata)
            // The schema "public." is typical for PostgreSQL. If using SQL Server, it might be "dbo."
            // Dapper parameter style @Identifier is generally cross-compatible.
            string sql = "SELECT * FROM public.Tenants WHERE \"LookupIdentifier\" = @Identifier;";
            // Note: Quoted "LookupIdentifier" for case-sensitivity in PostgreSQL if your column is cased.
            // If it's all lowercase in the DB (e.g., lookupidentifier), then no quotes are needed.

            try
            {
                // Use the factory to get an open connection to the TENANT METADATA database.
                // _multiTenancyOptions.Store.ConnectionStringName! ensures non-null, checked in constructor.
                await using DbConnection connection = await _dbConnectionFactory.CreateOpenConnectionAsync(_multiTenancyOptions.Store.ConnectionStringName!);

                TenantDbo? tenantDbo = await connection.QuerySingleOrDefaultAsync<TenantDbo>(sql, new { Identifier = identifier });

                if (tenantDbo == null)
                {
                    _logger.LogDebug("No tenant found in database for identifier '{Identifier}'.", identifier);
                    return null;
                }

                // Mapping DBO to ITenantInfo (logic remains the same as your original)
                Uri? logoUri = null;
                if (!string.IsNullOrWhiteSpace(tenantDbo.LogoUrl) &&
                    (!Uri.TryCreate(tenantDbo.LogoUrl, UriKind.Absolute, out logoUri) ||
                     (logoUri?.Scheme != Uri.UriSchemeHttp && logoUri?.Scheme != Uri.UriSchemeHttps)))
                {
                    _logger.LogWarning("Tenant '{TenantId}' from DB: Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.", tenantDbo.Id, tenantDbo.LogoUrl);
                    logoUri = null;
                }

                HashSet<string>? enabledFeatures = null;
                if (!string.IsNullOrWhiteSpace(tenantDbo.EnabledFeaturesJson))
                {
                    try { enabledFeatures = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(tenantDbo.EnabledFeaturesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                    catch (System.Text.Json.JsonException ex)
                    {
                        _logger.LogWarning(ex, "Tenant '{TenantId}' from DB: Failed to deserialize EnabledFeaturesJson. Value: '{JsonValue}'", tenantDbo.Id, tenantDbo.EnabledFeaturesJson);
                        // Error error = new("Tenant.Store.Db.Deserialization.EnabledFeatures", $"Failed to deserialize EnabledFeatures for tenant {tenantDbo.Id}.");
                        // No throw, proceed with empty.
                    }
                }

                Dictionary<string, string>? customProperties = null;
                if (!string.IsNullOrWhiteSpace(tenantDbo.CustomPropertiesJson))
                {
                     try { customProperties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(tenantDbo.CustomPropertiesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                    catch (System.Text.Json.JsonException ex)
                    {
                        _logger.LogWarning(ex, "Tenant '{TenantId}' from DB: Failed to deserialize CustomPropertiesJson. Value: '{JsonValue}'", tenantDbo.Id, tenantDbo.CustomPropertiesJson);
                        // Error error = new("Tenant.Store.Db.Deserialization.CustomProperties", $"Failed to deserialize CustomProperties for tenant {tenantDbo.Id}.");
                        // No throw, proceed with empty.
                    }
                }

                TenantInfo tenantInfo = new(
                    id: tenantDbo.Id,
                    name: tenantDbo.Name,
                    connectionStringName: tenantDbo.ConnectionStringName, // This is the *tenant's own* CS name, if applicable
                    status: (TenantStatus)tenantDbo.Status,
                    domain: tenantDbo.Domain,
                    subscriptionTier: tenantDbo.SubscriptionTier,
                    brandingName: tenantDbo.BrandingName,
                    logoUrl: logoUri,
                    dataIsolationMode: (TenantDataIsolationMode)tenantDbo.DataIsolationMode,
                    enabledFeatures: enabledFeatures,
                    customProperties: customProperties,
                    preferredLocale: tenantDbo.PreferredLocale,
                    timeZoneId: tenantDbo.TimeZoneId,
                    dataRegion: tenantDbo.DataRegion,
                    parentTenantId: tenantDbo.ParentTenantId,
                    createdAtUtc: tenantDbo.CreatedAtUtc,
                    updatedAtUtc: tenantDbo.UpdatedAtUtc,
                    concurrencyStamp: tenantDbo.ConcurrencyStamp
                );

                _logger.LogDebug("Tenant found in database for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.", identifier, tenantInfo.Id, tenantInfo.Status);
                return tenantInfo;
            }
            // Catch exceptions from the IDbConnectionFactory
            catch (ConnectionStringNotFoundException ex) // More specific than TenantConfigurationException for this case
            {
                Error error = new("Tenant.Store.Db.ConfigurationError", $"Configuration error for tenant metadata database: {ex.Message}");
                _logger.LogCritical(ex, error.Description);
                // This implies a fundamental setup issue for the store itself.
                throw new TenantConfigurationException(error, ex); // Wrapping it as TenantConfigurationException as it affects the store's setup
            }
            catch (UnsupportedDbProviderException ex)
            {
                Error error = new("Tenant.Store.Db.UnsupportedProvider", $"Unsupported DB provider for tenant metadata database: {ex.Message}");
                _logger.LogCritical(ex, error.Description);
                throw new TenantConfigurationException(error, ex);
            }
            catch (DbConnectionOpenException ex) // Specific exception for failure to open connection
            {
                Error error = new("Tenant.Store.Db.Unavailable", $"Tenant metadata database is unavailable: {ex.Message}");
                _logger.LogCritical(ex, error.Description);
                // This indicates the store itself is unable to connect to its backend.
                throw new TenantStoreUnavailableException(error, ex, $"ConnectionStringName: {_multiTenancyOptions.Store.ConnectionStringName}");
            }
            // Catch exceptions from Dapper/DB interaction
            catch (DbException ex) // Catches provider-specific exceptions from Dapper calls (e.g., SQL syntax error, table not found)
            {
                Error error = new("Tenant.Store.Db.QueryFailed", $"Database query failed while retrieving tenant by identifier '{identifier}'.");
                _logger.LogError(ex, error.Description);
                throw new TenantStoreQueryFailedException(error, ex, $"Identifier: {identifier}");
            }
            catch (System.Text.Json.JsonException jsonEx) // Catch deserialization errors for DTO properties
            {
                Error error = new("Tenant.Store.Db.DeserializationFailed", $"Failed to deserialize tenant data for identifier '{identifier}' from database response.");
                _logger.LogError(jsonEx, error.Description);
                // Assuming tenantDbo might be partially populated or null here.
                // The individual property deserialization already logs warnings.
                // This catch is more for a wholesale failure if JsonSerializer itself throws before specific property mapping.
                throw new TenantDeserializationException(error, jsonEx, nameof(TenantDbo));
            }
            catch (Exception ex) // Catch-all for other unexpected errors
            {
                Error error = new("Tenant.Store.Db.UnexpectedError", $"An unexpected error occurred in DatabaseTenantStore while retrieving tenant by identifier '{identifier}'.");
                _logger.LogError(ex, error.Description);
                throw new TenantStoreException(error, ex); // Generic store exception
            }
        }
    }
