using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class ConflictDomainException : DomainException
{
    public ConflictDomainException(Error error) : base(error) { }
    public ConflictDomainException(string message, Error error) : base(message, error) { }
    public ConflictDomainException(Error error, Exception innerException) : base(error, innerException) { }
    public ConflictDomainException(string message, Error error, Exception innerException) : base(message, error, innerException) { }
}
