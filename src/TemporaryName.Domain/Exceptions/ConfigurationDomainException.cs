using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class ConfigurationDomainException : DomainException
{
    public string ConfigurationKey { get; }

    public ConfigurationDomainException(string configurationKey, string? details = null)
        : base(new Error(
            "Configuration.InvalidOrMissing",
            details ?? $"A critical configuration value for '{configurationKey}' is missing or invalid. The application cannot proceed with this operation.",
            ErrorType.Unexpected, // This is a server-side setup problem
            new Dictionary<string, object> { { "configurationKey", configurationKey } }
        ))
    {
        ConfigurationKey = configurationKey;
    }
    
    public ConfigurationDomainException(Error error) : base(error)
    {
        ConfigurationKey = error.Metadata?.TryGetValue("configurationKey", out object? key) == true ? key.ToString() ?? "UnknownKey" : "UnknownKey";
    }

    public ConfigurationDomainException(string message, Error error) : base(message, error)
    {
        ConfigurationKey = error.Metadata?.TryGetValue("configurationKey", out object? key) == true ? key.ToString() ?? "UnknownKey" : "UnknownKey";
    }
}