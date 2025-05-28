using System;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Settings;

public class TenantStoreOptions
{
    /// <summary>
    /// Gets or sets the type of store to use for retrieving tenant definitions.
    /// Defaults to 'Configuration', using the inline 'Tenants' dictionary.
    /// </summary>
    public TenantStoreType Type { get; set; } = TenantStoreType.Configuration;

    /// <summary>
    /// Gets or sets the connection string name (from global configuration)
    /// to use if the <see cref="Type"/> is <see cref="TenantStoreType.Database"/>.
    /// </summary>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Gets or sets the base URI for the remote tenant management service
    /// if the <see cref="Type"/> is <see cref="TenantStoreType.RemoteService"/>.
    /// </summary>
    public string? ServiceEndpoint { get; set; }

    /// <summary>
    /// Gets or sets caching options for tenant information retrieved from the store.
    /// </summary>
    public CacheOptions Cache { get; set; } = new();
}
