using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemporaryName.Domain.Primitives.Outbox;
using TemporaryName.Infrastructure.Outbox.EFCore.Entities;

namespace TemporaryName.Infrastructure.Persistence.Common.EFCore.EntityTypeConfigurations;

/// <summary>
/// EF Core IEntityTypeConfiguration for the OutboxMessage entity.
/// This configuration is database-agnostic and applied by ApplicationDbContextBase.
/// Provider-specific DbContexts can further customize if needed, but common settings are here.
/// </summary>
public class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.ToTable("outbox_messages"); // Default table name, schema might be applied by context

        builder.HasKey(om => om.Id);
        builder.Property(om => om.Id).ValueGeneratedOnAdd();

        builder.Property(om => om.EventId).IsRequired();
        // Unique index on EventId is crucial to prevent accidental duplicate processing of the same domain event.
        builder.HasIndex(om => om.EventId).IsUnique();

        builder.Property(om => om.EventTypeFqn)
            .IsRequired()
            .HasMaxLength(512); // Ensure sufficient length for fully qualified type names.

        builder.Property(om => om.PayloadJson)
            .IsRequired();
            // Provider-specific DbContext (e.g., PostgreSqlApplicationDbContext) will configure
            // the column type (e.g., "jsonb" for PostgreSQL, "nvarchar(max)" for SQL Server).
            // Example for PostgreSQL (would be in PostgreSqlApplicationDbContext's OnModelCreating or its own config):
            // builder.Property(om => om.PayloadJson).HasColumnType("jsonb");

        builder.Property(om => om.OccurredAtUtc).IsRequired();
        builder.Property(om => om.PersistedAtUtc).IsRequired();
        // Index for querying/cleanup jobs, or for fallback pollers.
        builder.HasIndex(om => om.PersistedAtUtc).HasDatabaseName("IX_OutboxMessages_PersistedAtUtc");

        builder.Property(om => om.AggregateType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(om => om.AggregateId)
            .IsRequired()
            .HasMaxLength(256); // Or a length appropriate for your aggregate IDs.

        builder.Property(om => om.AggregateVersion); // Nullable long

        builder.Property(om => om.CorrelationId); // Nullable Guid
        builder.Property(om => om.CausationId);   // Nullable Guid

        builder.Property(om => om.UserId).HasMaxLength(256);   // Nullable string
        builder.Property(om => om.TenantId).HasMaxLength(128); // Nullable string

        // Provider-specific DbContext will configure column type for JSON properties.
        // builder.Property(om => om.TraceContextJson).HasColumnType("jsonb"); // Example for PostgreSQL
        builder.Property(om => om.TraceContextJson); // Nullable

        builder.Property(om => om.ProtoSchemaVersion)
            .IsRequired()
            .HasMaxLength(32);

        // builder.Property(om => om.MetadataJson).HasColumnType("jsonb"); // Example for PostgreSQL
        builder.Property(om => om.MetadataJson); // Nullable

        builder.Property(om => om.Status)
               .IsRequired()
               .HasConversion<string>() // Store enum as string for better readability in DB and cross-platform compatibility.
               .HasMaxLength(50);
        // Index for fallback pollers or monitoring tools to find pending/failed messages.
        builder.HasIndex(om => new { om.Status, om.PersistedAtUtc })
               .HasDatabaseName("IX_OutboxMessages_Status_PersistedAtUtc");

        builder.Property(om => om.ProcessAttemptCount)
               .IsRequired()
               .HasDefaultValue(0); // Ensure it defaults to 0 on creation.

        builder.Property(om => om.LastProcessAttemptAtUtc); // Nullable DateTime

        builder.Property(om => om.LastProcessError)
               .HasMaxLength(4000); // Max length for error messages, adjust as needed.
    }
}
