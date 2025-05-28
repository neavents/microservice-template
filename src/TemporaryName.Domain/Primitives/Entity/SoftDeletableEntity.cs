using System;
using TemporaryName.Domain.Primitives.Deletable;

namespace TemporaryName.Domain.Primitives.Entity;

public abstract class SoftDeletableEntity<TId> : Entity<TId>, ISoftDeletable
where TId : IEquatable<TId>
{
    private bool _isDeleted;
    private DateTimeOffset? _deletedAtUtc;
    private DateTimeOffset? _lastRestoredAtUtc;
    private string? _deletedBy;
    private string? _lastRestoredBy;

    bool ISoftDeletable.IsDeleted { get => _isDeleted; set => _isDeleted = value; }
    DateTimeOffset? ISoftDeletable.DeletedAtUtc { get => _deletedAtUtc; set => _deletedAtUtc = value; }
    DateTimeOffset? ISoftDeletable.LastRestoredAtUtc { get => _lastRestoredAtUtc; set => _lastRestoredAtUtc = value; }
    string? ISoftDeletable.DeletedBy { get => _deletedBy; set => _deletedBy = value; }
    string? ISoftDeletable.LastRestoredBy { get => _lastRestoredBy; set => _lastRestoredBy = value; }
}
