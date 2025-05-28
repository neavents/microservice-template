using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

public class TenantResolutionException : MultiTenancyException
{
    public TenantResolutionException(Error error) : base(error) { }
    public TenantResolutionException(Error error, Exception innerException) : base(error, innerException) { }
    public TenantResolutionException(string message, Error error) : base(message, error) { }
    public TenantResolutionException(string message, Error error, Exception innerException) : base(message, error, innerException) { }
}
