using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Represents errors that occur when interacting with the tenant store.
/// </summary>
public class TenantStoreException : MultiTenancyException
{
    public TenantStoreException(Error error) : base(error) { }
    public TenantStoreException(Error error, Exception innerException) : base(error, innerException) { }
    public TenantStoreException(string message, Error error) : base(message, error) { }
    public TenantStoreException(string message, Error error, Exception innerException) : base(message, error, innerException) { }
}
