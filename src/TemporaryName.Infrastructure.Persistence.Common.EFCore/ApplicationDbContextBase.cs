using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TemporaryName.Infrastructure.Outbox.EFCore.Entities;
using TemporaryName.Infrastructure.Persistence.Common.EFCore.EntityTypeConfigurations;
using TemporaryName.Infrastructure.Persistence.Common.EFCore.Extensions;

namespace TemporaryName.Infrastructure.Persistence.Common.EFCore;

/// <summary>
/// Base DbContext class containing shared DbSet properties and common model configurations.
/// Provider-specific DbContexts will inherit from this class.
/// </summary>
public abstract class ApplicationDbContextBase : DbContext
{

    protected ApplicationDbContextBase(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.ConfigureSnakeCaseNaming(); 
    }
}
