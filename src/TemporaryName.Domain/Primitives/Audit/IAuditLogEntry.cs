using System;

namespace TemporaryName.Domain.Primitives.Audit;

public interface IAuditLogEntry
{
    Guid AuditEntryId { get; }         // Unique ID for the audit entry itself
    DateTimeOffset Timestamp { get; }   // When the audit entry was recorded
    string? UserId { get; }            // Who performed the action
    string EntityId { get; }            // ID of the entity affected (can be string for flexibility)
    string EntityType { get; }          // Type name of the entity affected
    string Action { get; }              // e.g., "CREATE", "UPDATE", "DELETE", "ACCESS", "LOGIN_FAIL"
    string? Changeset { get; }         // JSON diff of old/new values for UPDATEs
    string? Reason { get; }            // Business reason for the change, if applicable
    string? CorrelationId { get; }     // To link related operations across services/requests
    string? TransactionId { get; }     // Business transaction ID
    string? IpAddress { get; }
    string? UserAgent { get; }
}
