using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class ConcurrencyDomainException : DomainException
{
    public string ResourceType { get; }
    public object ResourceIdentifier { get; }

    public ConcurrencyDomainException(string resourceType, object resourceIdentifier, string? attemptedAction = null)
        : base(new Error(
            "Concurrency.Conflict",
            $"A concurrency conflict occurred while attempting to '{attemptedAction ?? "modify"}' " +
            $"the {resourceType} with identifier '{resourceIdentifier}'. The resource may have been updated by another process. Please refresh and try again.",
            ErrorType.Conflict, // Maps to HTTP 409 Conflict
            new Dictionary<string, object?> {
                { "resourceType", resourceType },
                { "resourceIdentifier", resourceIdentifier },
                { "attemptedAction", attemptedAction ?? "modify" }
            }
        ))
    {
        ResourceType = resourceType;
        ResourceIdentifier = resourceIdentifier;
    }
    
    public ConcurrencyDomainException(Error error) : base(error)
    {
        ResourceType = error.Metadata?.TryGetValue("resourceType", out object? rt) == true ? rt.ToString() ?? "UnknownResource" : "UnknownResource";
        ResourceIdentifier = error.Metadata?.TryGetValue("resourceIdentifier", out object? ri) == true ? ri : "UnknownIdentifier";
    }

    public ConcurrencyDomainException(string message, Error error) : base(message, error)
    {
        ResourceType = error.Metadata?.TryGetValue("resourceType", out object? rt) == true ? rt.ToString() ?? "UnknownResource" : "UnknownResource";
        ResourceIdentifier = error.Metadata?.TryGetValue("resourceIdentifier", out object? ri) == true ? ri : "UnknownIdentifier";
    }
}

