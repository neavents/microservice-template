using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TemporaryName.Infrastructure.Outbox.EFCore.Entities;
using TemporaryName.Infrastructure.Persistence.Common.EFCore.EntityTypeConfigurations;
using TemporaryName.Infrastructure.Persistence.Common.EFCore.Extensions;

namespace TemporaryName.Infrastructure.Persistence.Common.EFCore;

public abstract class OutboxEnabledDbContextBase : DbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected OutboxEnabledDbContextBase(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityTypeConfiguration());
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.ConfigureSnakeCaseNaming();
    }
}
