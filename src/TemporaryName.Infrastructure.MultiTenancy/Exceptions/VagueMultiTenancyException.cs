using System;
using SharedKernel.Primitives;

namespace TemporaryName.Infrastructure.MultiTenancy.Exceptions;

public class VagueMultiTenancyException : MultiTenancyException
{
    public VagueMultiTenancyException(Error error) : base(error) { }
    public VagueMultiTenancyException(Error error, Exception innerException) : base(error, innerException) { }
    public VagueMultiTenancyException(string message, Error error) : base(message, error) { }
    public VagueMultiTenancyException(string message, Error error, Exception innerException) : base(message, error, innerException) { }
}
