using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when data retrieved from the tenant store cannot be deserialized into a tenant information object.
/// </summary>
public class TenantDeserializationException : TenantStoreException
{
    public string? AttemptedDataType { get; } // e.g., ITenantInfo, TenantConfigurationEntry

    public TenantDeserializationException(Error error, string? attemptedDataType = null) : base(error)
    {
        AttemptedDataType = attemptedDataType;
    }
    public TenantDeserializationException(Error error, Exception innerException, string? attemptedDataType = null) : base(error, innerException)
    {
        AttemptedDataType = attemptedDataType;
    }
}
