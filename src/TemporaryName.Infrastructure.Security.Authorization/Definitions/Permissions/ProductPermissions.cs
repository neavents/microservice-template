using System;

namespace TemporaryName.Infrastructure.Security.Authorization.Definitions.Permissions;

public static class ProductsPermissions
{
    private const string GroupName = "Permissions.Products"; // Base for this group
    public const string View = $"{GroupName}.View";
    public const string Create = $"{GroupName}.Create";
    public const string Edit = $"{GroupName}.Edit";
    public const string Delete = $"{GroupName}.Delete";
    public const string ManageStock = $"{GroupName}.ManageStock";
}
