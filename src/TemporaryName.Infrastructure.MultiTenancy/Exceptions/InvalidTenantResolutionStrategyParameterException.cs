using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

/// <summary>
/// Thrown when a configured tenant resolution strategy has invalid parameters (e.g., a required ParameterName is missing for HttpHeader or QueryString strategy).
/// </summary>
public class InvalidTenantResolutionStrategyParameterException : TenantConfigurationException
{
    public string? StrategyType { get; }
    public string? MissingParameter { get; }

    public InvalidTenantResolutionStrategyParameterException(Error error, string? strategyType = null, string? missingParameter = null) : base(error)
    {
        StrategyType = strategyType;
        MissingParameter = missingParameter;
    }
    public InvalidTenantResolutionStrategyParameterException(Error error, Exception innerException, string? strategyType = null, string? missingParameter = null) : base(error, innerException)
    {
        StrategyType = strategyType;
        MissingParameter = missingParameter;
    }
}
