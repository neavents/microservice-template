using System;
using SharedKernel.Primitives;

namespace TemporaryName.Domain.Exceptions;

public class BusinessRuleViolationDomainException : DomainException
{
    public BusinessRuleViolationDomainException(Error error) : base(error) { }
    public BusinessRuleViolationDomainException(string message, Error error) : base(message, error) { }
    public BusinessRuleViolationDomainException(Error error, Exception innerException) : base(error, innerException) { }
    public BusinessRuleViolationDomainException(string message, Error error, Exception innerException) : base(message, error, innerException) { }
}
