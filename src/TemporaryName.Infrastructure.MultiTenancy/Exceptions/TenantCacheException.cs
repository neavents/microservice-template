using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Represents errors that occur during tenant information caching operations.
/// </summary>
public class TenantCacheException : MultiTenancyException
{
    public string? CacheKey { get; }
    public string? OperationAttempted { get; } // e.g., "Get", "Set", "Remove"

    public TenantCacheException(Error error, string? cacheKey = null, string? operationAttempted = null) : base(error)
    {
        CacheKey = cacheKey;
        OperationAttempted = operationAttempted;
    }
    public TenantCacheException(Error error, Exception innerException, string? cacheKey = null, string? operationAttempted = null) : base(error, innerException)
    {
        CacheKey = cacheKey;
        OperationAttempted = operationAttempted;
    }
}
