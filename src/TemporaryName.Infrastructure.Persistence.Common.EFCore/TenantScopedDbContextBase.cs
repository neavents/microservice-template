using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Primitives;
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;
using TemporaryName.Infrastructure.MultiTenancy.Configuration;
using TemporaryName.Infrastructure.MultiTenancy.Exceptions;

namespace TemporaryName.Infrastructure.Persistence.Common.EFCore;

public abstract class TenantScopedDbContextBase : DbContext
{
    protected readonly ILogger<TenantScopedDbContextBase> _logger;
    protected readonly TenantDataOptions _tenantDataOptions;
    protected readonly string? _dbContextResolvedTenantId;

    /// <summary>
    /// Gets the Tenant ID that this DbContext instance is scoped to.
    /// This is resolved once when the DbContext is created based on the ambient ITenantContext.
    /// Will be null if no active tenant was resolved at instantiation.
    /// </summary>
    public string? CurrentDbContextTenantId => _dbContextResolvedTenantId;

    protected TenantScopedDbContextBase(
        DbContextOptions options,
        ITenantContext tenantContext,
        IOptionsMonitor<TenantDataOptions> tenantDataOptionsAccessor,
        ILogger<TenantScopedDbContextBase> logger)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(tenantContext, nameof(tenantContext));
        ArgumentNullException.ThrowIfNull(tenantDataOptionsAccessor, nameof(tenantDataOptionsAccessor));
        ArgumentNullException.ThrowIfNull(tenantDataOptionsAccessor.CurrentValue, nameof(tenantDataOptionsAccessor.CurrentValue));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _logger = logger;
        _tenantDataOptions = tenantDataOptionsAccessor.CurrentValue;

        if (tenantContext.IsTenantResolvedAndActive && !string.IsNullOrWhiteSpace(tenantContext.CurrentTenant?.Id))
        {
            _dbContextResolvedTenantId = tenantContext.CurrentTenant.Id;
            _logger.LogDebug("TenantScopedDbContextBase: Initialized. Operating under Tenant ID '{TenantId}'.", _dbContextResolvedTenantId);
        }
        else
        {
            _dbContextResolvedTenantId = null;
            _logger.LogWarning("TenantScopedDbContextBase: Initialized without an active Tenant ID. Tenant-scoped operations will be restricted.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        string tenantIdPropertyName = nameof(ITenantScopedEntity.TenantId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(et => typeof(ITenantScopedEntity).IsAssignableFrom(et.ClrType)))
        {
            _logger.LogDebug("Applying tenant filter to {EntityType} using DbContext's resolved Tenant ID: '{TenantIdForFilter}'",
                entityType.ClrType.FullName, _dbContextResolvedTenantId ?? "NULL (strict filtering)");

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression propertyAccess = Expression.Property(parameter, tenantIdPropertyName);

            Expression filterBody;
            if (_dbContextResolvedTenantId is not null)
            {
                ConstantExpression tenantIdConstant = Expression.Constant(_dbContextResolvedTenantId, typeof(string));
                filterBody = Expression.Equal(propertyAccess, tenantIdConstant);
            }
            else
            {
                filterBody = Expression.Constant(false);
            }
            // 4. Create the lambda expression
            LambdaExpression lambda = Expression.Lambda(filterBody, parameter);

            // 5. Apply the filter
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            _logger.LogTrace("Applied query filter to {EntityType}. Effective filter logic depends on DbContext's resolved TenantId ('{TenantId}')",
                entityType.ClrType.FullName, _dbContextResolvedTenantId ?? "NULL -> restrictive");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantIdAndValidate();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantIdAndValidate();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies TenantId to new entities and validates TenantId on modified entities
    /// based on the DbContext's resolved TenantId and configured TenantDataOptions.
    /// This method is virtual and can be overridden for specialized behavior, though caution is advised.
    /// </summary>
    protected virtual void ApplyTenantIdAndValidate()
    {
        var tenantScopedEntries = ChangeTracker.Entries<ITenantScopedEntity>().ToList();
        if (tenantScopedEntries.Count == 0) return;

        _logger.LogDebug("ApplyTenantIdAndValidate: Processing {Count} ITenantScopedEntity entries. DbContext TenantId: '{DbContextTenantId}'",
            tenantScopedEntries.Count, _dbContextResolvedTenantId ?? "NULL");

        foreach (var entry in tenantScopedEntries)
        {
            if (entry.State == EntityState.Added)
            {
                HandleAddedTenantScopedEntity(entry);
            }
            else if (entry.State == EntityState.Modified)
            {
                HandleModifiedTenantScopedEntity(entry);
            }
        }
    }

    protected virtual void HandleAddedTenantScopedEntity(EntityEntry<ITenantScopedEntity> entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        
        string entityTypeName = entry.Metadata.ClrType.Name;
        if (string.IsNullOrWhiteSpace(_dbContextResolvedTenantId))
        {
            Error error = Error.Problem( // Using a more specific ErrorType
                "Tenant.Persistence.Add.MissingDbContextTenantId",
                $"Cannot save new entity '{entityTypeName}' because DbContext has no resolved Tenant ID.",
                new Dictionary<string, object?> { { "EntityType", entityTypeName } }
            );
            _logger.LogError("{ErrorCode}: {ErrorDescription} Metadata: {ErrorMetadata}", error.Code, error.Description, error.Metadata);
            throw new TenantDataPersistenceException(error); // Pass the Error object
        }

        if (string.IsNullOrWhiteSpace(entry.Entity.TenantId))
        {
            entry.Entity.TenantId = _dbContextResolvedTenantId;
            _logger.LogTrace("Set TenantId='{TenantId}' on new entity of type {EntityType}", _dbContextResolvedTenantId, entityTypeName);
        }
        else if (entry.Entity.TenantId != _dbContextResolvedTenantId)
        {
            _logger.LogWarning("New entity '{EntityType}' added with pre-set TenantId '{EntityTenantId}', which mismatches DbContext's TenantId '{DbContextTenantId}'. Behavior: {Behavior}",
                entityTypeName, entry.Entity.TenantId, _dbContextResolvedTenantId, _tenantDataOptions.NewEntityMismatchedTenantIdBehavior);

            switch (_tenantDataOptions.NewEntityMismatchedTenantIdBehavior)
            {
                case MismatchedTenantIdResolution.OverrideWithContextTenantId:
                    entry.Entity.TenantId = _dbContextResolvedTenantId;
                    _logger.LogInformation("Overrode TenantId on new entity '{EntityType}' to match DbContext TenantId '{DbContextTenantId}'.", entityTypeName, _dbContextResolvedTenantId);
                    break;
                case MismatchedTenantIdResolution.ThrowException:
                default:
                    Error error = Error.Conflict( // Using Error.Conflict for this type of issue
                        "Tenant.Persistence.Add.MismatchedTenantId",
                        $"New entity '{entityTypeName}' has TenantId '{entry.Entity.TenantId}', conflicting with DbContext's TenantId '{_dbContextResolvedTenantId}'.",
                        new Dictionary<string, object?> {
                                { "EntityType", entityTypeName },
                                { "EntityTenantId", entry.Entity.TenantId },
                                { "ContextTenantId", _dbContextResolvedTenantId }
                        }
                    );
                    _logger.LogError("{ErrorCode}: {ErrorDescription} Metadata: {ErrorMetadata}", error.Code, error.Description, error.Metadata);
                    throw new TenantDataPersistenceException(error);
            }
        }
    }

    protected virtual void HandleModifiedTenantScopedEntity(EntityEntry<ITenantScopedEntity> entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string entityTypeName = entry.Metadata.ClrType.Name;
        PropertyEntry tenantIdProperty = entry.Property(e => e.TenantId);

        if (tenantIdProperty.IsModified)
        {
            Error error = Error.Validation( // Using Error.Validation as it's a rule violation
                "Tenant.Persistence.Modify.TenantIdModificationForbidden",
                $"Attempted to modify TenantId on existing entity '{entityTypeName}' from '{tenantIdProperty.OriginalValue}' to '{tenantIdProperty.CurrentValue}'. This is forbidden.",
                 new Dictionary<string, object?> {
                        { "EntityType", entityTypeName },
                        { "OriginalTenantId", tenantIdProperty.OriginalValue },
                        { "AttemptedTenantId", tenantIdProperty.CurrentValue }
                }
            );
            _logger.LogError("{ErrorCode}: {ErrorDescription} Metadata: {ErrorMetadata}", error.Code, error.Description, error.Metadata);
            throw new TenantDataPersistenceException(error);
        }

        if (_dbContextResolvedTenantId != null && entry.Entity.TenantId != _dbContextResolvedTenantId)
        {
            Error error = Error.Problem(
                "Tenant.Persistence.Modify.IntegrityViolation",
                $"Modified entity '{entityTypeName}' (TenantId: {entry.Entity.TenantId}) does not match DbContext's resolved TenantId ('{_dbContextResolvedTenantId}'). Potential data integrity issue.",
                new Dictionary<string, object?> {
                        { "EntityType", entityTypeName },
                        { "EntityTenantId", entry.Entity.TenantId },
                        { "ContextTenantId", _dbContextResolvedTenantId }
                }
            );
            _logger.LogError("{ErrorCode}: {ErrorDescription} Metadata: {ErrorMetadata}", error.Code, error.Description, error.Metadata);
            throw new TenantDataPersistenceException(error);
        }
    }
    

    public IQueryable<TEntity> GetHostAccessibleSet<TEntity>() where TEntity : class
    {
        _logger.LogWarning("Accessing DbSet for {EntityType} via GetHostAccessibleSet, bypassing global tenant filters. STRICT AUTHORIZATION REQUIRED.", typeof(TEntity).Name);
        return Set<TEntity>().IgnoreQueryFilters();
    }
}
