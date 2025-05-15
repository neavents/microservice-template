using System;

namespace TemporaryName.Domain.Primitives.Contextual;

public interface IVersionable
{
    long Version { get; }
}
