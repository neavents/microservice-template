using System;

namespace TemporaryName.Domain.Primitives.Audit;

public interface IModifiedAuditable
{
    DateTimeOffset LastModifiedAtUtc { get; }
    string? LastModifiedBy { get; }
    int? VersionForAudit { get; }
    int ModifiedCount { get; protected set;}

    void IncreaseModifiedCount(){
        ModifiedCount++;
    }
}
