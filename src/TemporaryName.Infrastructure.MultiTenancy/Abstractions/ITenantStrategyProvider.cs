using System;
using TemporaryName.Infrastructure.MultiTenancy.Settings;

namespace TemporaryName.Infrastructure.MultiTenancy.Abstractions;

/// <summary>
/// Defines a contract for providing instances of <see cref="ITenantIdentificationStrategy"/>
/// based on configuration.
/// </summary>
public interface ITenantStrategyProvider
{
    /// <summary>
    /// Gets an instance of <see cref="ITenantIdentificationStrategy"/> for the given options.
    /// </summary>
    /// <param name="strategyOptions">The configuration for the desired strategy.</param>
    /// <returns>An instance of the configured tenant identification strategy.</returns>
    /// <exception cref="TenantConfigurationException">
    /// Thrown if the strategy type is unknown or if required parameters for the strategy are missing/invalid.
    /// </exception>
    public ITenantIdentificationStrategy GetStrategy(TenantResolutionStrategyOptions strategyOptions);
}
