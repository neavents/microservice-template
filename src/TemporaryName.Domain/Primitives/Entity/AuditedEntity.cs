using System;
using TemporaryName.Domain.Primitives.Audit;

namespace TemporaryName.Domain.Primitives.Entity;

public class AuditedEntity<TId> : SoftDeletableEntity<TId>, IAuditable
where TId : IEquatable<TId>
{
    private DateTimeOffset _lastModifiedAtUtc;
    private DateTimeOffset _createdAtUtc;
    private string? _lastModifiedBy;
    private string? _createdBy;
    private int? _versionForAudit;
    private int _modifiedCount;

    public DateTimeOffset LastModifiedAtUtc => _lastModifiedAtUtc;

    public string? LastModifiedBy => _lastModifiedBy;

    public int? VersionForAudit => _versionForAudit;

    public DateTimeOffset CreatedAtUtc => _createdAtUtc;

    public string? CreatedBy => _createdBy;

    public int ModifiedCount { get => _modifiedCount; set => _modifiedCount = value; }
}
