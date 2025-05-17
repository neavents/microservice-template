using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TemporaryName.Infrastructure.Security.Authorization.Definitions.Permissions;
using TemporaryName.Infrastructure.Security.Authorization.Definitions.Policies;
using TemporaryName.Infrastructure.Security.Authorization.Requirements;

namespace TemporaryName.Infrastructure.Security.Authorization.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddAppAuthorizationCore(
            this IServiceCollection services,
            Action<AuthorizationOptions>? configureExtraAuthorizationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        AuthorizationBuilder authorizationBuilder = services.AddAuthorizationBuilder();
        ConfigureApplicationPolicies(authorizationBuilder);

        if (configureExtraAuthorizationOptions != null)
        {
            services.Configure(configureExtraAuthorizationOptions);
        }
        return services;
    }

    private static void ConfigureApplicationPolicies(AuthorizationBuilder builder)
    {
        // --- General Policies ---
        builder.AddPolicy(AdministrationPolicyNames.AccessDashboard, policy => // Using new path
            policy.AddRequirements(new HasPermissionRequirement(GeneralPermissions.AccessAdminDashboard))); // Using new path

        // --- Product Policies ---
        builder.AddPolicy(ProductsPolicyNames.ViewProducts, policy =>
            policy.AddRequirements(new HasPermissionRequirement(ProductsPermissions.View)));
        builder.AddPolicy(ProductsPolicyNames.CreateProducts, policy =>
            policy.AddRequirements(new HasPermissionRequirement(ProductsPermissions.Create)));
        builder.AddPolicy(ProductsPolicyNames.EditProducts, policy =>
            policy.AddRequirements(new HasPermissionRequirement(ProductsPermissions.Edit)));
        builder.AddPolicy(ProductsPolicyNames.DeleteProducts, policy =>
            policy.AddRequirements(new HasPermissionRequirement(ProductsPermissions.Delete)));
        builder.AddPolicy(ProductsPolicyNames.ManageProductStock, policy =>
            policy.AddRequirements(new HasPermissionRequirement(ProductsPermissions.ManageStock)));

        builder.AddPolicy(ProductsPolicyNames.FullProductManagement, policy =>
            policy.AddRequirements(new HasAllPermissionsRequirement(new[]
            {
                ProductsPermissions.View,
                ProductsPermissions.Create,
                ProductsPermissions.Edit,
                ProductsPermissions.Delete,
                ProductsPermissions.ManageStock
            })));

        // --- Order Policies ---
        builder.AddPolicy(OrdersPolicyNames.CreateOrders, policy =>
            policy.AddRequirements(new HasPermissionRequirement(OrdersPermissions.Create)));
        builder.AddPolicy(OrdersPolicyNames.UpdateOrderStatus, policy =>
            policy.AddRequirements(new HasPermissionRequirement(OrdersPermissions.EditStatus)));
        builder.AddPolicy(OrdersPolicyNames.ViewAllOrders, policy =>
            policy.AddRequirements(new HasPermissionRequirement(OrdersPermissions.ViewAll)));
        builder.AddPolicy(OrdersPolicyNames.CancelOrders, policy =>
            policy.AddRequirements(new HasPermissionRequirement(OrdersPermissions.Cancel)));

        // Example of combining role (from JWT) and permission
        builder.AddPolicy(OrdersPolicyNames.ManagerCanCancelOrders, policy =>
            policy.RequireRole("order-manager") // Assumes 'order-manager' role claim exists in JWT
                  .AddRequirements(new HasPermissionRequirement(OrdersPermissions.Cancel)));

        // Example of using HasAnyPermissionRequirement
        builder.AddPolicy(CustomersPolicyNames.ViewOrEditCustomers, policy =>
           policy.AddRequirements(new HasAnyPermissionRequirement(new[]
           {
               CustomersPermissions.View,
               CustomersPermissions.Edit
           })));
    }

}
