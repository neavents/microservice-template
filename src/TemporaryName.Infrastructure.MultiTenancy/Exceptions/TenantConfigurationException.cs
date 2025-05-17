using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Represents errors that occur due to invalid or missing multi-tenancy configuration.
/// </summary>
public class TenantConfigurationException : MultiTenancyException
{
    public TenantConfigurationException(Error error) : base(error) { }
    public TenantConfigurationException(Error error, Exception innerException) : base(error, innerException) { }
    public TenantConfigurationException(string message, Error error) : base(message, error) { }
    public TenantConfigurationException(string message, Error error, Exception innerException) : base(message, error, innerException) { }
}
