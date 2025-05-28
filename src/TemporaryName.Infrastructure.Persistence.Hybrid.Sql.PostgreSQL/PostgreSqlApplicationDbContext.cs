
using Microsoft.EntityFrameworkCore;
using TemporaryName.Infrastructure.Persistence.Common.EFCore;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;

public class PostgreSqlApplicationDbContext : OutboxEnabledDbContextBase
{
    public PostgreSqlApplicationDbContext(DbContextOptions<PostgreSqlApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        // 1. Apply all common configurations from ApplicationDbContextBase
        base.OnModelCreating(modelBuilder);

        // 2. Apply PostgreSQL-specific configurations
        modelBuilder.HasDefaultSchema("public"); // Example: Set default schema for PostgreSQL

        // Example: Configure specific column types for JSON properties for PostgreSQL
        modelBuilder.Entity<TemporaryName.Infrastructure.Outbox.EFCore.Entities.OutboxMessage>(b =>
        {
            b.Property(om => om.PayloadJson).HasColumnType("jsonb");
            b.Property(om => om.TraceContextJson).HasColumnType("jsonb");
            b.Property(om => om.MetadataJson).HasColumnType("jsonb");
        });

        // Add other PostgreSQL specific configurations:
        // - Extensions (e.g., modelBuilder.HasPostgresExtension("uuid-ossp");)
        // - Specific indexing methods (e.g., GIN indexes for jsonb columns)
        // - Sequences if not handled by HiLo or other value generation strategies
        // - Custom data type mappings specific to Npgsql

        // Example: If you used a common snake_case naming convention in base, it's already applied.
        // If you need to override or apply a different one specifically for PostgreSQL:
        // ConfigureSnakeCaseNaming(modelBuilder); // Could be called here too if base doesn't do it.
    }
}
