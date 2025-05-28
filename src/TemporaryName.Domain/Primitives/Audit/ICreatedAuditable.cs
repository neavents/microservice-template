using System;

namespace TemporaryName.Domain.Primitives.Audit;

public interface ICreatedAuditable
{
    DateTimeOffset CreatedAtUtc { get; }
    string? CreatedBy { get; } // User ID, system process ID, API key ID
}
