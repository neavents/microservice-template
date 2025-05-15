using System;

namespace TemporaryName.Domain.Primitives.Contextual;

public interface IContainsMetadata
{
    IReadOnlyDictionary<string, object> GetMetadata();
}
