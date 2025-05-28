namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public enum TenantStoreType
{
    /// <summary>
    /// Tenant definitions are loaded directly from the application's configuration
    /// (e.g., from the 'Tenants' dictionary in MultiTenancyOptions).
    /// </summary>
    Configuration = 0,

    /// <summary>
    /// Tenant definitions are loaded from a database.
    /// Connection details are typically specified via ConnectionStringName.
    /// </summary>
    Database = 1,

    /// <summary>
    /// Tenant definitions are loaded from a remote service or API.
    /// Endpoint details are specified via ServiceEndpoint.
    /// </summary>
    RemoteService = 2,

    /// <summary>
    /// A custom ITenantStore implementation is provided by the application.
    /// The system will look for a registered service implementing ITenantStore.
    /// </summary>
    Custom = 3
}
