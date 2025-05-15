using System;

namespace TemporaryName.Domain.Primitives.Contextual;

public interface IUserContextualEvent
{
    string? UserId { get; }
}
