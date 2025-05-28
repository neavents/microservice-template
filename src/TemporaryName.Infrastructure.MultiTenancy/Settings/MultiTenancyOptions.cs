using System;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Settings;

    public class MultiTenancyOptions
    {
        /// <summary>
        /// The default configuration section name for these options.
        /// </summary>
        public const string ConfigurationSectionName = "MultiTenancy";

        /// <summary>
        /// Gets or sets a value indicating whether multi-tenancy is enabled.
        /// If false, tenant resolution middleware might be skipped, and a null or default tenant context might be used.
        /// Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default tenant identifier to use if no tenant can be resolved from the request
        /// via any of the configured <see cref="ResolutionStrategies"/>, and if a fallback is desired.
        /// If null or empty, and <see cref="ThrowIfTenantMissing"/> is true, requests without an identifiable
        /// tenant will result in an error.
        /// </summary>
        public string? DefaultTenantIdentifier { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to throw a TenantResolutionException (or similar)
        /// if a tenant cannot be identified through any strategy AND no <see cref="DefaultTenantIdentifier"/>
        /// is configured or found.
        /// If false, ITenantContext.CurrentTenant will be null in such cases, and the application must handle it.
        /// Defaults to true (fail fast if tenant is required for the request path).
        /// </summary>
        public bool ThrowIfTenantMissing { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of strategies to use for resolving the tenant from the current request.
        /// Strategies are attempted in the order specified by their <see cref="TenantResolutionStrategyOptions.Order"/> property.
        /// </summary>
        public List<TenantResolutionStrategyOptions> ResolutionStrategies { get; set; } = new();

        /// <summary>
        /// Gets or sets the configuration for the tenant store, specifying where tenant definitions are loaded from
        /// (e.g., configuration, database, remote service) and caching options.
        /// </summary>
        public TenantStoreOptions Store { get; set; } = new();

        /// <summary>
        /// Gets or sets a dictionary of tenant configurations, keyed by tenant identifier.
        /// This is primarily used if <see cref="TenantStoreOptions.Type"/> is set to <see cref="TenantStoreType.Configuration"/>.
        /// It allows defining tenants directly within the application's configuration files (e.g., appsettings.json).
        /// </summary>
        public Dictionary<string, TenantConfigurationEntry> Tenants { get; set; } = new();

        /// <summary>
        /// Gets or sets default settings (e.g., locale, timezone, data region) to apply to any resolved tenant
        /// if those settings are not explicitly defined for that tenant in its own configuration.
        /// This is distinct from <see cref="DefaultTenantIdentifier"/>, which is a fallback tenant ID.
        /// </summary>
        public DefaultTenantSettingsOptions DefaultSettings { get; set; } = new();

        /// <summary>
        /// Gets or sets options for handling requests that do not map to a specific tenant
        /// (e.g., a central landing page, global admin functionalities).
        /// </summary>
        public HostHandlingOptions HostHandling { get; set; } = new();
    }
