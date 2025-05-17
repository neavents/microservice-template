using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when a query to the tenant store fails for reasons other than unavailability.
/// </summary>
public class TenantStoreQueryFailedException : TenantStoreException
{
    public string? QueryDetails { get; } // e.g., "Fetching tenant by ID"

    public TenantStoreQueryFailedException(Error error, string? queryDetails = null) : base(error)
    {
        QueryDetails = queryDetails;
    }
    public TenantStoreQueryFailedException(Error error, Exception innerException, string? queryDetails = null) : base(error, innerException)
    {
        QueryDetails = queryDetails;
    }
}
