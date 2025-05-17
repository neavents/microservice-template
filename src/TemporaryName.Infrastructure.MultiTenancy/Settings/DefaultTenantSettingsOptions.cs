using System;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;

namespace TemporaryName.Infrastructure.MultiTenancy.Settings;

    public class DefaultTenantSettingsOptions
    {
        /// <summary>
        /// Gets or sets the default preferred locale (e.g., "en-US") if not set per tenant.
        /// </summary>
        public string? PreferredLocale { get; set; }

        /// <summary>
        /// Gets or sets the default time zone ID (e.g., "UTC", "Europe/Istanbul") if not set per tenant.
        /// </summary>
        public string? TimeZoneId { get; set; }

        /// <summary>
        /// Gets or sets the default data region if not set per tenant.
        /// </summary>
        public string? DataRegion { get; set; }

        /// <summary>
        /// Gets or sets the default subscription tier if not set per tenant.
        /// </summary>
        public string? SubscriptionTier { get; set; }

        /// <summary>
        /// Gets or sets the default data isolation mode if not set per tenant.
        /// </summary>
        public TenantDataIsolationMode? DataIsolationMode { get; set; }
    }
