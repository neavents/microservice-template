using System;

namespace TemporaryName.Domain.Primitives.Contextual;

public interface ICorrelated
{
    Guid? CorrelationId { get; }
}
