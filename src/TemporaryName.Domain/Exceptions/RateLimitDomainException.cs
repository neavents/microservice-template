using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class RateLimitDomainException : DomainException
{
    public string ResourceOrOperation { get; }
    public TimeSpan? RetryAfter { get; }

    public RateLimitDomainException(string resourceOrOperation, string message, TimeSpan? retryAfter = null)
        : base(new Error(
            "RateLimit.Exceeded",
            message,
            ErrorType.Failure, // Maps to HTTP 429 Too Many Requests via its specific mapper
            new Dictionary<string, object?> {
                { "resource", resourceOrOperation },
                { "retryAfterSeconds", retryAfter?.TotalSeconds}
            }
        ))
    {
        ResourceOrOperation = resourceOrOperation;
        RetryAfter = retryAfter;
    }
    
    public RateLimitDomainException(Error error, TimeSpan? retryAfter = null) : base(error)
    {
        ResourceOrOperation = error.Metadata?.TryGetValue("resource", out object? res) == true ? res?.ToString() ?? "UnknownResource" : "UnknownResource";
        RetryAfter = retryAfter ?? (error.Metadata?.TryGetValue("retryAfterSeconds", out object? seconds) == true && seconds is double dSeconds ? TimeSpan.FromSeconds(dSeconds) : null);
    }

    public RateLimitDomainException(string message, Error error, TimeSpan? retryAfter = null) : base(message, error)
    {
        ResourceOrOperation = error.Metadata?.TryGetValue("resource", out object? res) == true ? res?.ToString() ?? "UnknownResource" : "UnknownResource";
        RetryAfter = retryAfter ?? (error.Metadata?.TryGetValue("retryAfterSeconds", out object? seconds) == true && seconds is double dSeconds ? TimeSpan.FromSeconds(dSeconds) : null);
    }
}

