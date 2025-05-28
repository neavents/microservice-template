using System;

namespace TemporaryName.Domain.Primitives;

public interface IIdentifiable<out TId>
{
    TId Id { get; }
}
