using System;
using System.Reflection;

namespace TemporaryName.Infrastructure.Security.Authorization.Definitions.Permissions;


public static class AllDefinedPermissions
{
    /// <summary>
    /// Retrieves a list of all defined permission constants from classes within this namespace
    /// that end with "Permissions".
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        List<string> allPermissions = new();
        // Get all public static classes in the same namespace (or sub-namespaces if needed)
        // that define permissions. A convention like ending class names with "Permissions" helps.
        IEnumerable<Type> permissionDefiningTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && // static classes are abstract sealed
                          t.Namespace != null &&
                          (t.Namespace.StartsWith(typeof(AllDefinedPermissions).Namespace ?? "") || // Permissions in same namespace or sub-namespace
                           t.Namespace.StartsWith("TemporaryName.Infrastructure.Security.Authorization.Definitions.Permissions")) &&
                           t.Name.EndsWith("Permissions"));

        foreach (Type? type in permissionDefiningTypes)
        {
            if (type == null) continue;
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            allPermissions.AddRange(fields
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string) && fi.GetRawConstantValue() is string)
                .Select(fi => (string)fi.GetRawConstantValue()!));
        }
        return allPermissions.Distinct().ToList().AsReadOnly();
    }
}
