using System;
using System.Data.Common;
using System.Text.Json;
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
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<DatabaseTenantStore> _logger;
        private readonly MultiTenancyOptions _multiTenancyOptions;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        public DatabaseTenantStore(
            IDbConnectionFactory dbConnectionFactory, 
            IOptions<MultiTenancyOptions> multiTenancyOptionsAccessor,
            ILogger<DatabaseTenantStore> logger)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory, nameof(dbConnectionFactory));
            ArgumentNullException.ThrowIfNull(multiTenancyOptionsAccessor, nameof(multiTenancyOptionsAccessor));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
            _multiTenancyOptions = multiTenancyOptionsAccessor.Value;

            _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (_multiTenancyOptions == null)
            {
                Error error = new("MultiTenancy.Configuration.OptionsAccessorValueNull.DbStore", "IOptions<MultiTenancyOptions>.Value is null. DatabaseTenantStore cannot be initialized.");
                _logger.LogCritical(error.Description);
                throw new TenantConfigurationException(error.Description, error);
            }

            if (_multiTenancyOptions.Store.Type != TenantStoreType.Database)
            {
                _logger.LogWarning("DatabaseTenantStore is registered, but MultiTenancyOptions.Store.Type is '{StoreType}'. This store might not be used as intended by the configuration.", _multiTenancyOptions.Store.Type);
            }

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
                // The factory to get an open connection to the TENANT METADATA database.
                // _multiTenancyOptions.Store.ConnectionStringName! ensures non-null, checked in constructor.
                await using DbConnection connection = await _dbConnectionFactory.CreateOpenConnectionAsync(_multiTenancyOptions.Store.ConnectionStringName!).ConfigureAwait(false);

                DatabaseTenantDto? tenantDatabaseDto = await connection.QuerySingleOrDefaultAsync<DatabaseTenantDto>(sql, new { Identifier = identifier });

                if (tenantDatabaseDto == null)
                {
                    _logger.LogDebug("No tenant found in database for identifier '{Identifier}'.", identifier);
                    return null;
                }

                Uri? logoUri = null;
                if (!string.IsNullOrWhiteSpace(tenantDatabaseDto.LogoUrl) &&
                    (!Uri.TryCreate(tenantDatabaseDto.LogoUrl, UriKind.Absolute, out logoUri) ||
                     (logoUri?.Scheme != Uri.UriSchemeHttp && logoUri?.Scheme != Uri.UriSchemeHttps)))
                {
                    _logger.LogWarning("Tenant '{TenantId}' from DB: Invalid or non-HTTP/HTTPS LogoUrl '{LogoUrl}'. It will be ignored.", tenantDatabaseDto.Id, tenantDatabaseDto.LogoUrl);
                    logoUri = null;
                }

                HashSet<string>? enabledFeatures = null;
                if (!string.IsNullOrWhiteSpace(tenantDatabaseDto.EnabledFeaturesJson))
                {
                    try { enabledFeatures = JsonSerializer.Deserialize<HashSet<string>>(tenantDatabaseDto.EnabledFeaturesJson, _jsonSerializerOptions); }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Tenant '{TenantId}' from DB: Failed to deserialize EnabledFeaturesJson. Value: '{JsonValue}'", tenantDatabaseDto.Id, tenantDatabaseDto.EnabledFeaturesJson);
                        // Error error = new("Tenant.Store.Db.Deserialization.EnabledFeatures", $"Failed to deserialize EnabledFeatures for tenant {tenantDbo.Id}.");
                        // No throw, proceed with empty.
                    }
                }

                Dictionary<string, string>? customProperties = null;
                if (!string.IsNullOrWhiteSpace(tenantDatabaseDto.CustomPropertiesJson))
                {
                     try { customProperties = JsonSerializer.Deserialize<Dictionary<string, string>>(tenantDatabaseDto.CustomPropertiesJson, _jsonSerializerOptions); }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Tenant '{TenantId}' from DB: Failed to deserialize CustomPropertiesJson. Value: '{JsonValue}'", tenantDatabaseDto.Id, tenantDatabaseDto.CustomPropertiesJson);
                        // Error error = new("Tenant.Store.Db.Deserialization.CustomProperties", $"Failed to deserialize CustomProperties for tenant {tenantDbo.Id}.");
                        // No throw, proceed with empty.
                    }
                }

                TenantInfo tenantInfo = new(
                    id: tenantDatabaseDto.Id,
                    name: tenantDatabaseDto.Name,
                    connectionStringName: tenantDatabaseDto.ConnectionStringName,
                    status: (TenantStatus)tenantDatabaseDto.Status,
                    domain: tenantDatabaseDto.Domain,
                    subscriptionTier: tenantDatabaseDto.SubscriptionTier,
                    brandingName: tenantDatabaseDto.BrandingName,
                    logoUrl: logoUri,
                    dataIsolationMode: (TenantDataIsolationMode)tenantDatabaseDto.DataIsolationMode,
                    enabledFeatures: enabledFeatures,
                    customProperties: customProperties,
                    preferredLocale: tenantDatabaseDto.PreferredLocale,
                    timeZoneId: tenantDatabaseDto.TimeZoneId,
                    dataRegion: tenantDatabaseDto.DataRegion,
                    parentTenantId: tenantDatabaseDto.ParentTenantId,
                    createdAtUtc: tenantDatabaseDto.CreatedAtUtc,
                    updatedAtUtc: tenantDatabaseDto.UpdatedAtUtc,
                    concurrencyStamp: tenantDatabaseDto.ConcurrencyStamp
                );

                _logger.LogDebug("Tenant found in database for identifier '{Identifier}'. Tenant ID: '{TenantId}', Status: '{TenantStatus}'.", identifier, tenantInfo.Id, tenantInfo.Status);
                return tenantInfo;
            }
           
            catch (ConnectionStringNotFoundException ex) 
            {
                Error error = new("Tenant.Store.Db.ConfigurationError", $"Configuration error for tenant metadata database: {ex.Message}");
                _logger.LogCritical(ex, error.Description);
            
                throw new TenantConfigurationException(error, ex); /
            }
            catch (UnsupportedDbProviderException ex)
            {
                Error error = new("Tenant.Store.Db.UnsupportedProvider", $"Unsupported DB provider for tenant metadata database: {ex.Message}");
                _logger.LogCritical(ex, error.Description);
                throw new TenantConfigurationException(error, ex);
            }
            catch (DbConnectionOpenException ex) 
            {
                Error error = new("Tenant.Store.Db.Unavailable", $"Tenant metadata database is unavailable: {ex.Message}");
                _logger.LogCritical(ex, error.Description);
 
                throw new TenantStoreUnavailableException(error, ex, $"ConnectionStringName: {_multiTenancyOptions.Store.ConnectionStringName}");
            }
            catch (DbException ex)
            {
                Error error = new("Tenant.Store.Db.QueryFailed", $"Database query failed while retrieving tenant by identifier '{identifier}'.");
                _logger.LogError(ex, error.Description);
                throw new TenantStoreQueryFailedException(error, ex, $"Identifier: {identifier}");
            }
            catch (JsonException jsonEx) 
            {
                Error error = new("Tenant.Store.Db.DeserializationFailed", $"Failed to deserialize tenant data for identifier '{identifier}' from database response.");
                _logger.LogError(jsonEx, error.Description);

                throw new TenantDeserializationException(error, jsonEx, nameof(DatabaseTenantDto));
            }
            catch (Exception ex) 
            {
                Error error = new("Tenant.Store.Db.UnexpectedError", $"An unexpected error occurred in DatabaseTenantStore while retrieving tenant by identifier '{identifier}'.");
                _logger.LogError(ex, error.Description);
                throw new TenantStoreException(error, ex); 
            }
        }
    }
