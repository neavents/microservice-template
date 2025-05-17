using System;

namespace TemporaryName.Infrastructure.Security.Authorization.Definitions.Policies;

public static class ProductsPolicyNames
{
    private const string GroupName = "Policy.Products";
    public const string ViewProducts = $"{GroupName}.View";
    public const string CreateProducts = $"{GroupName}.Create";
    public const string EditProducts = $"{GroupName}.Edit";
    public const string DeleteProducts = $"{GroupName}.Delete";
    public const string ManageProductStock = $"{GroupName}.ManageStock";
    public const string FullProductManagement = $"{GroupName}.FullManagement";
}
