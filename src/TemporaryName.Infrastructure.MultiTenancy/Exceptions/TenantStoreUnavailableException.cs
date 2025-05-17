using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when the configured tenant store (e.g., database, remote service) is unavailable.
/// </summary>
public class TenantStoreUnavailableException : TenantStoreException
{
    public string? StoreDetails { get; } // e.g., connection string name, service endpoint

    public TenantStoreUnavailableException(Error error, string? storeDetails = null) : base(error)
    {
        StoreDetails = storeDetails;
    }
    public TenantStoreUnavailableException(Error error, Exception innerException, string? storeDetails = null) : base(error, innerException)
    {
        StoreDetails = storeDetails;
    }
}
