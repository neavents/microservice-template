using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when a tenant identifier is successfully extracted from the request, but no tenant with that identifier exists in the store.
/// </summary>
public class TenantNotFoundException : TenantResolutionException
{
    public string AttemptedTenantIdentifier { get; }

    public TenantNotFoundException(string attemptedTenantIdentifier, Error error) : base(error)
    {
        AttemptedTenantIdentifier = attemptedTenantIdentifier;
    }
    public TenantNotFoundException(string attemptedTenantIdentifier, Error error, Exception innerException) : base(error, innerException)
    {
        AttemptedTenantIdentifier = attemptedTenantIdentifier;
    }
}
