// File: TemporaryName.Infrastructure.MultiTenancy/Persistence/TenantQueryFilterExtensions.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TemporaryName.Domain.Primitives; // Assuming ITenantSpecific is here
using TemporaryName.Infrastructure.MultiTenancy.Abstractions;

namespace TemporaryName.Infrastructure.MultiTenancy.Persistence;

/// <summary>
/// Provides extension methods for applying tenant-specific global query filters to Entity Framework Core DbContexts.
/// </summary>
public static class TenantQueryFilterExtensions
{
    private static readonly MethodInfo _propertyMethod = typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Static | BindingFlags.Public)!;

    /// <summary>
    /// Configures global query filters for all entities that implement <see cref="ITenantSpecific"/>.
    /// This ensures that queries automatically filter data based on the current tenant ID from <see cref="ITenantContext"/>.
    /// This method should be called within the OnModelCreating method of your DbContext.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to construct the model for the context.</param>
    /// <param name="tenantContext">The <see cref="ITenantContext"/> providing the current tenant ID.
    /// This instance is typically captured in the DbContext constructor via DI.</param>
    /// <exception cref="ArgumentNullException">If modelBuilder or tenantContext is null.</exception>
    public static ModelBuilder ApplyTenantQueryFilters(this ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        ArgumentNullException.ThrowIfNull(tenantContext, nameof(tenantContext));

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if the entity type implements ITenantSpecific
            // This requires ITenantSpecific to have a TenantId property (e.g., string or Guid)
            if (typeof(ITenantSpecific).IsAssignableFrom(entityType.ClrType))
            {
                PropertyInfo? tenantIdPropertyInfo = entityType.ClrType.GetProperty(nameof(ITenantSpecific.TenantId));
                if (tenantIdPropertyInfo == null)
                {
                    throw new InvalidOperationException(
                        $"Entity type {entityType.DisplayName()} implements {nameof(ITenantSpecific)} but does not have a public '{nameof(ITenantSpecific.TenantId)}' property.");
                }

                // Create the lambda expression for the query filter: e => EF.Property<TId>(e, "TenantId") == tenantContext.CurrentTenant.Id
                ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");

                // EF.Property<TId>(e, "TenantId")
                // Ensure the shadow property name matches what's configured (or actual property if not shadow)
                // For simplicity, assuming "TenantId" is the name of the property (shadow or actual).
                // The type of TenantId (string, Guid, etc.) must match ITenantInfo.Id.
                MemberExpression propertyAccess = Expression.Property(parameter, tenantIdPropertyInfo);
                // If using a shadow property:
                // MethodCallExpression efPropertyCall = Expression.Call(
                //    null,
                //    _propertyMethod.MakeGenericMethod(tenantIdPropertyInfo.PropertyType), // Use actual property type
                //    parameter,
                //    Expression.Constant(nameof(ITenantSpecific.TenantId)));


                // tenantContext.CurrentTenant.Id
                // This part needs to be carefully constructed to be translatable by EF Core.
                // Accessing tenantContext.CurrentTenant.Id directly in the lambda might not always work if tenantContext
                // is not a DbContext property or a parameter that EF Core can understand.
                // A common pattern is to have the TenantId available as a property on the DbContext itself,
                // which is set when the DbContext is instantiated for the current request scope.

                // For this example, let's assume tenantContext.CurrentTenant.Id can be accessed and is of the correct type.
                // This will be translated to a parameter in the SQL query.
                // IMPORTANT: The type of ITenantInfo.Id must match the type of ITenantSpecific.TenantId
                Expression<Func<string?>> currentTenantIdExpression = () => tenantContext.CurrentTenant!.Id; // Note the non-null assertion

                // Build the comparison: EF.Property<TId>(e, "TenantId") == _tenantIdFromContext
                BinaryExpression equality = Expression.Equal(propertyAccess, Expression.Convert(currentTenantIdExpression.Body, propertyAccess.Type));

                LambdaExpression lambda = Expression.Lambda(equality, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
        return modelBuilder;
    }
}

// In your DbContext (e.g., PostgreSqlApplicationDbContext.cs):
// public class YourApplicationDbContext : DbContext // or your base DbContext
// {
//     private readonly ITenantContext _tenantContext;
//     private readonly string? _currentTenantId; // Store resolved tenant ID for use in filters

//     public YourApplicationDbContext(DbContextOptions<YourApplicationDbContext> options, ITenantContext tenantContext)
//         : base(options)
//     {
//         _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
//         _currentTenantId = _tenantContext.CurrentTenant?.Id; // Capture at instantiation
//     }

//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         base.OnModelCreating(modelBuilder);

//         // Apply tenant query filters
//         // Pass the DbContext's captured _currentTenantId to a modified ApplyTenantQueryFilters
//         // or adjust ApplyTenantQueryFilters to use a Func<string> that captures _currentTenantId.
//         // For simplicity, the provided ApplyTenantQueryFilters uses ITenantContext directly,
//         // which relies on ITenantContext being correctly scoped and accessible during query execution.
//         // A more robust EF Core way is to pass the scalar tenant ID value.

//         modelBuilder.ApplyTenantQueryFilters(_tenantContext); // Original approach
//         // OR, if you modify ApplyTenantQueryFilters to take the ID directly:
//         // modelBuilder.ApplyTenantQueryFilters(_currentTenantId);
//     }

//     // Example of how ITenantSpecific might be defined in your Domain project:
//     // namespace TemporaryName.Domain.Primitives;
//     // public interface ITenantSpecific
//     // {
//     //     string TenantId { get; set; } // Or Guid, ensure type matches ITenantInfo.Id
//     // }
// }
```
**Note on `ApplyTenantQueryFilters`:** The direct use of `tenantContext.CurrentTenant.Id` inside the lambda expression for `HasQueryFilter` can sometimes be tricky for EF Core's translation depending on how `ITenantContext` is scoped and resolved. A more common and robust pattern for EF Core global query filters is to have the `DbContext` capture the `TenantId` as a scalar property during its construction (from the scoped `ITenantContext`) and then use that `DbContext` property in the query filter lambda.

Modified `ApplyTenantQueryFilters` and `DbContext` for the more robust scalar `TenantId` approach:
```csharp
// In TenantQueryFilterExtensions.cs
public static ModelBuilder ApplyTenantQueryFiltersWithScalarTenantId(this ModelBuilder modelBuilder, string? currentTenantId)
{
    ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));

    // If currentTenantId is null, and entities are tenant-specific, this means either:
    // 1. This is a host/system operation that should not be filtered (requires more logic to bypass filter).
    // 2. No tenant is resolved, and tenant-specific data should not be accessible.
    // For simplicity, if currentTenantId is null, the filter `e.TenantId == null` might be applied,
    // or you might choose to throw if a tenant is strictly required for all DB operations.
    // Let's assume for now that if currentTenantId is null, no tenant-specific data should match.

    foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantSpecific).IsAssignableFrom(entityType.ClrType))
        {
            PropertyInfo? tenantIdPropertyInfo = entityType.ClrType.GetProperty(nameof(ITenantSpecific.TenantId));
            if (tenantIdPropertyInfo == null) continue; // Or throw

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression propertyAccess = Expression.Property(parameter, tenantIdPropertyInfo);
            
            // If currentTenantId is null, create a filter that likely returns no data (e.g., e.TenantId == "a-non-existent-guid-or-value")
            // or handle as per your application's requirements for non-tenanted access.
            // A simple approach: if currentTenantId is null, the equality check will handle it if TenantId is non-nullable.
            // If TenantId is nullable, you might need `e.TenantId == null`.
            // Forcing a match on currentTenantId:
            ConstantExpression tenantIdConstant = Expression.Constant(currentTenantId, tenantIdPropertyInfo.PropertyType);
            BinaryExpression equality = Expression.Equal(propertyAccess, tenantIdConstant);
            LambdaExpression lambda = Expression.Lambda(equality, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
    return modelBuilder;
}

// In YourApplicationDbContext.cs
// ...
// private readonly string? _currentTenantId;
// public YourApplicationDbContext(DbContextOptions<YourApplicationDbContext> options, ITenantContext tenantContext) {
//     _tenantContext = tenantContext;
//     _currentTenantId = tenantContext.CurrentTenant?.Id; // Captured at construction
// }
// protected override void OnModelCreating(ModelBuilder modelBuilder) {
//     base.OnModelCreating(modelBuilder);
//     modelBuilder.ApplyTenantQueryFiltersWithScalarTenantId(_currentTenantId);
// }
// ...
```
This scalar approach is generally preferred for EF Core query filte