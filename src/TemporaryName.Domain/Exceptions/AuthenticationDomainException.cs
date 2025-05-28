using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class AuthenticationDomainException : DomainException
{
    public AuthenticationDomainException(Error error)
        : base(error.Type == ErrorType.Unauthorized ? error : new Error(error.Code, error.Description, ErrorType.Unauthorized, error.Metadata))
    { }

    public AuthenticationDomainException(string message, Error error)
        : base(message, error.Type == ErrorType.Unauthorized ? error : new Error(error.Code, error.Description, ErrorType.Unauthorized, error.Metadata))
    { }

    public AuthenticationDomainException(string reason, string? errorCode = null)
        : base(new Error(
            errorCode ?? "Authentication.DomainFailure",
            reason,
            ErrorType.Unauthorized, // Typically maps to HTTP 401
            new Dictionary<string, object> { { "authenticationFailureReason", reason } }
        ))
    { }
}
