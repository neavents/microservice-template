using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.DataAccess.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;
using TemporaryName.Infrastructure.MultiTenancy.Implementations.Stores;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations.Providers;

/// <summary>
/// Provides instances of <see cref="ITenantStore"/> based on configuration,
/// potentially wrapping them with caching.
/// </summary>
public partial class TenantStoreProvider : ITenantStoreProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantStoreProvider> _logger;

    public TenantStoreProvider(IServiceProvider serviceProvider, ILogger<TenantStoreProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ITenantStore GetStore(TenantStoreOptions storeOptions)
    {
        ArgumentNullException.ThrowIfNull(storeOptions, nameof(storeOptions));

        _logger.LogDebug("TenantStoreProvider: Getting base store for type: {StoreType}, ConnectionStringName: '{CSN}', ServiceEndpoint: '{Endpoint}'",
            storeOptions.Type, storeOptions.ConnectionStringName, storeOptions.ServiceEndpoint);

        ITenantStore baseStore;
        try
        {
            switch (storeOptions.Type)
            {
                case TenantStoreType.Configuration:
                    baseStore = new ConfigurationTenantStore(
                        (IOptionsMonitor<MultiTenancyOptions>)_serviceProvider.GetService(typeof(IOptionsMonitor<MultiTenancyOptions>))!,
                        (ILogger<ConfigurationTenantStore>)_serviceProvider.GetService(typeof(ILogger<ConfigurationTenantStore>))!
                    );
                    break;
                case TenantStoreType.Database:
                    baseStore = new DatabaseTenantStore(
                        (IDbConnectionFactory)_serviceProvider.GetService(typeof(IDbConnectionFactory))!,
                        (IOptions<MultiTenancyOptions>)_serviceProvider.GetService(typeof(IOptions<MultiTenancyOptions>))!,
                        (ILogger<DatabaseTenantStore>)_serviceProvider.GetService(typeof(ILogger<DatabaseTenantStore>))!
                    );
                    break;
                case TenantStoreType.RemoteService:
                    baseStore = new RemoteHttpTenantStore(
                        (IHttpClientFactory)_serviceProvider.GetService(typeof(IHttpClientFactory))!,
                        (IOptions<MultiTenancyOptions>)_serviceProvider.GetService(typeof(IOptions<MultiTenancyOptions>))!,
                        (ILogger<RemoteHttpTenantStore>)_serviceProvider.GetService(typeof(ILogger<RemoteHttpTenantStore>))!
                    );
                    break;
                case TenantStoreType.Custom:
                    ITenantStore? customStore = (ITenantStore?)_serviceProvider.GetService(typeof(ITenantStore));
                    // Check if the resolved store is one of the "default" types, implying a specific custom one wasn't registered or was overridden.
                    if (customStore == null ||
                        customStore.GetType() == typeof(ConfigurationTenantStore) ||
                        customStore.GetType() == typeof(DatabaseTenantStore) ||
                        customStore.GetType() == typeof(RemoteHttpTenantStore) ||
                        customStore.GetType() == typeof(InMemoryTenantStore)
                        )
                    {
                        Error error = new("Tenant.Store.Custom.NotDistinctlyRegistered", "TenantStoreType.Custom is configured, but no distinct custom ITenantStore implementation was found or it resolved to a default store type. Ensure your custom store is registered and uniquely identifiable if needed.");
                        _logger.LogCritical(error.Description);
                        throw new TenantConfigurationException(error.Description, error);
                    }
                    baseStore = customStore;
                    _logger.LogInformation("TenantStoreProvider: Using custom registered ITenantStore of type {CustomStoreType}", baseStore.GetType().FullName);
                    break;
                default:
                    Error err = new("Tenant.Store.UnknownType", $"Unsupported tenant store type: {storeOptions.Type}.");
                    _logger.LogCritical(err.Description);
                    throw new TenantConfigurationException(err.Description, err);
            }
        }
        catch (TenantConfigurationException ex)
        {
            _logger.LogError(ex, "TenantStoreProvider: Failed to create tenant store due to configuration issues for type {StoreType}.", storeOptions.Type);
            throw;
        }
        catch (Exception ex)
        {
            Error error = new("Tenant.Store.InstantiationFailed", $"TenantStoreProvider: Failed to instantiate tenant store of type {storeOptions.Type}. See inner exception for details.");
            _logger.LogCritical(ex, error.Description);
            throw new TenantConfigurationException(error.Description, error, ex);
        }
        _logger.LogInformation("TenantStoreProvider: Successfully created base store of type {StoreTypeResolved}.", baseStore.GetType().FullName);
        return baseStore;
    }
}
