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

        LogGettingBaseStoreInfo(_logger, storeOptions.Type, storeOptions.ConnectionStringName, storeOptions.ServiceEndpoint);

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
                    if (customStore == null ||
                        customStore.GetType() == typeof(ConfigurationTenantStore) ||
                        customStore.GetType() == typeof(DatabaseTenantStore) ||
                        customStore.GetType() == typeof(RemoteHttpTenantStore) ||
                        customStore.GetType() == typeof(InMemoryTenantStore)
                        )
                    {
                        Error error = new("Tenant.Store.Custom.NotDistinctlyRegistered", "TenantStoreType.Custom is configured, but no distinct custom ITenantStore implementation was found or it resolved to a default store type. Ensure your custom store is registered and uniquely identifiable if needed.");
                        LogCustomStoreNotDistinctlyRegistered(_logger, error.Code, error.Description);

                        throw new TenantConfigurationException(error.Description!, error);
                    }
                    baseStore = customStore;
                    LogUsingCustomStore(_logger, baseStore.GetType());

                    break;
                default:
                    Error err = new("Tenant.Store.UnknownType", $"Unsupported tenant store type: {storeOptions.Type}.");
                    LogUnsupportedStoreType(_logger, storeOptions.Type, err.Code, err.Description);
                    throw new TenantConfigurationException(err.Description!, err);
            }
        }
        catch (TenantConfigurationException ex)
        {
            LogStoreCreationConfigError(_logger, storeOptions.Type, ex);
            throw;
        }
        catch (Exception ex)
        {
            Error error = new("Tenant.Store.InstantiationFailed", $"TenantStoreProvider: Failed to instantiate tenant store of type {storeOptions.Type}. See inner exception for details.");

            LogStoreInstantiationFailed(_logger, storeOptions.Type, error.Code, error.Description, ex);

            throw new TenantConfigurationException(error.Description!, error, ex);
        }
        
        LogBaseStoreCreated(_logger, baseStore.GetType());
        return baseStore;
    }
}
