using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class ForbiddenAccessDomainException : DomainException
{
    public ForbiddenAccessDomainException(Error error)
        : base(error.Type == ErrorType.Forbidden ? error : new Error(error.Code, error.Description, ErrorType.Forbidden, error.Metadata))
    { }

    public ForbiddenAccessDomainException(string message, Error error)
        : base(message, error.Type == ErrorType.Forbidden ? error : new Error(error.Code, error.Description, ErrorType.Forbidden, error.Metadata))
    { }
    
    public ForbiddenAccessDomainException(string userId, string resourceOrAction)
        : base(new Error(
            "Access.Forbidden",
            $"User '{userId}' is forbidden from accessing or performing action on '{resourceOrAction}'.",
            ErrorType.Forbidden,
            new Dictionary<string, object> { { "userId", userId }, { "resourceOrAction", resourceOrAction } }
        ))
    { }
}
