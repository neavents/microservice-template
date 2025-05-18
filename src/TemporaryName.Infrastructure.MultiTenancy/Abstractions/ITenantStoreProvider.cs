using System;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

/// <summary>
/// Defines a contract for providing instances of <see cref="ITenantStore"/>.
/// </summary>
public interface ITenantStoreProvider
{
    /// <summary>
    /// Gets an instance of <see cref="ITenantStore"/> based on the configured options.
    /// This may return a cached version of the store.
    /// </summary>
    /// <param name="storeOptions">The configuration for the tenant store.</param>
    /// <returns>An instance of the configured tenant store.</returns>
    /// <exception cref="TenantConfigurationException">
    /// Thrown if the store type is unknown or if required parameters for the store are missing/invalid.
    /// </exception>
    public ITenantStore GetStore(TenantStoreOptions storeOptions);
}
