using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when no tenant resolution strategies are defined in the configuration, but the system expects at least one to identify tenants.
/// </summary>
public class TenantResolutionStrategyMissingException : TenantConfigurationException
{
    public TenantResolutionStrategyMissingException(Error error) : base(error) { }
    public TenantResolutionStrategyMissingException(Error error, Exception innerException) : base(error, innerException) { }
}
