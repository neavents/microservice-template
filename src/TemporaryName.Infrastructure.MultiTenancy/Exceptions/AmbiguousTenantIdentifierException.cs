using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown if the resolution process identifies multiple tenants for a single request, indicating an ambiguous or conflicting configuration.
/// </summary>
public class AmbiguousTenantIdentifierException : TenantResolutionException
{
    public string AmbiguousIdentifier { get; }

    public AmbiguousTenantIdentifierException(string ambiguousIdentifier, Error error) : base(error)
    {
        AmbiguousIdentifier = ambiguousIdentifier;
    }
    public AmbiguousTenantIdentifierException(string ambiguousIdentifier, Error error, Exception innerException) : base(error, innerException)
    {
        AmbiguousIdentifier = ambiguousIdentifier;
    }
}
