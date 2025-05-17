using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when a specific resolution strategy is attempted but the necessary context (e.g., required HTTP header, query parameter) is missing from the request.
/// </summary>
public class TenantResolutionStrategyNotApplicableException : TenantResolutionException
{
    public string StrategyType { get; }
    public string? MissingContextInfo { get; } // e.g., "Header 'X-Tenant-ID' not found"

    public TenantResolutionStrategyNotApplicableException(string strategyType, Error error, string? missingContextInfo = null) : base(error)
    {
        StrategyType = strategyType;
        MissingContextInfo = missingContextInfo;
    }
    public TenantResolutionStrategyNotApplicableException(string strategyType, Error error, Exception innerException, string? missingContextInfo = null) : base(error, innerException)
    {
        StrategyType = strategyType;
        MissingContextInfo = missingContextInfo;
    }
}
