using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

public class TenantInvalidException : TenantResolutionException
{
    public string TenantIdentifier { get; }

    public TenantInvalidException(string tenantIdentifier, Error error) : base(error)
    {
        TenantIdentifier = tenantIdentifier;
    }

    public TenantInvalidException(string tenantIdentifier, Error error, Exception innerException) : base(error, innerException)
    {
        TenantIdentifier = tenantIdentifier;
    }
    
    public TenantInvalidException(string tenantIdentifier, string message, Error error) : base(message, error)
    {
        TenantIdentifier = tenantIdentifier;
    }
}
