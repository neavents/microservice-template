using System;

namespace TemporaryName.Domain.Primitives.Contextual;

public interface ICaused
{
    Guid? CausationId { get; }
}
