using System;

namespace TemporaryName.Infrastructure.Security.Authorization.Definitions.Policies;

public static class AdministrationPolicyNames
{
    private const string GroupName = "Policy.Admin";
    public const string AccessDashboard = $"{GroupName}.AccessDashboard";
}
