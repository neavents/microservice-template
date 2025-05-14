using System;

namespace TemporaryName.Domain.Primitives.Deletable;

public interface ISoftDeletable
{
    public bool IsDeleted { get; protected set;}
    public DateTimeOffset? DeletedAtUtc { get; protected set; } 
    public DateTimeOffset? LastRestoredAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }
    public string? LastRestoredBy {get; protected set;}

    void SoftDelete(string? userId, DateTimeOffset timestamp){
        if(IsDeleted) return;

        IsDeleted = true;
        DeletedAtUtc = timestamp;
        DeletedBy = userId;
    }

    void Restore(string? userId, DateTimeOffset timestamp){
        if(!IsDeleted) return;

        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
        LastRestoredAtUtc = timestamp;
        LastRestoredBy = userId;
    }

    bool IsDeletedBefore(){
        if(IsDeleted) return true;
        if(LastRestoredAtUtc is not null || LastRestoredBy is not null) return true;

        return false; 
    }

    bool IsRestoredBefore(){
        if(LastRestoredAtUtc is not null || LastRestoredBy is not null) return true;

        return false;
    }
}
