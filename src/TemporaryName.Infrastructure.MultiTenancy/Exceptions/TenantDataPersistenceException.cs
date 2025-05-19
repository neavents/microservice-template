using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

public class TenantDataPersistenceException : MultiTenancyException
{
    public string? EntityTypeAttempted { get; }
    public TenantDataPersistenceException(Error error, string? entityTypeAttempted = null) : base(error) { EntityTypeAttempted = entityTypeAttempted; }
    public TenantDataPersistenceException(string message, Error error, string? entityTypeAttempted = null) : base(message, error) { EntityTypeAttempted = entityTypeAttempted; }
    public TenantDataPersistenceException(Error error, Exception innerException, string? entityTypeAttempted = null) : base(error, innerException) { EntityTypeAttempted = entityTypeAttempted; }
    public TenantDataPersistenceException(string message, Error error, Exception innerException, string? entityTypeAttempted = null) : base(message, error, innerException) { EntityTypeAttempted = entityTypeAttempted; }
}
