using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class NotFoundDomainException : DomainException
{
    public string ResourceName { get; }
    public object ResourceIdentifier { get; }

    public NotFoundDomainException(string resourceName, object resourceIdentifier, string? customMessage = null)
        : base(customMessage ?? $"The {resourceName} with identifier '{resourceIdentifier}' was not found.",
               new Error(
                   $"{resourceName}.NotFound",
                   customMessage ?? $"The {resourceName} with identifier '{resourceIdentifier}' was not found.",
                   ErrorType.NotFound,
                   new Dictionary<string, object> { { "resource", resourceName }, { "identifier", resourceIdentifier } }
               ))
    {
        ResourceName = resourceName;
        ResourceIdentifier = resourceIdentifier;
    }

    public NotFoundDomainException(Error error) : base(error)
    {
        ResourceName = error.Metadata?.TryGetValue("resource", out object? rn) == true ? rn.ToString() ?? "UnknownResource" : "UnknownResource";
        ResourceIdentifier = error.Metadata?.TryGetValue("identifier", out object? ri) == true ? ri : "UnknownIdentifier";
    }
    
    public NotFoundDomainException(string message, Error error) : base(message, error)
    {
        ResourceName = error.Metadata?.TryGetValue("resource", out object? rn) == true ? rn.ToString() ?? "UnknownResource" : "UnknownResource";
        ResourceIdentifier = error.Metadata?.TryGetValue("identifier", out object? ri) == true ? ri : "UnknownIdentifier";
    }
}
