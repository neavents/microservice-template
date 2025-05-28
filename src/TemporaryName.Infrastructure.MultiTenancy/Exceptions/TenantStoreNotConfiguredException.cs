using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when a required tenant store (e.g., Database, RemoteService) is not configured or its configuration is incomplete.
/// </summary>
public class TenantStoreNotConfiguredException : TenantConfigurationException
{
    public string? MissingConfigurationKey { get; }

    public TenantStoreNotConfiguredException(Error error, string? missingConfigurationKey = null) : base(error)
    {
        MissingConfigurationKey = missingConfigurationKey;
    }
    public TenantStoreNotConfiguredException(Error error, Exception innerException, string? missingConfigurationKey = null) : base(error, innerException)
    {
        MissingConfigurationKey = missingConfigurationKey;
    }
}