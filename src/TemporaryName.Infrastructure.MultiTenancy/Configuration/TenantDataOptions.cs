using System;

namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public class TenantDataOptions
{
    public const string ConfigurationSectionName = "MultiTenancy:TenantData";
    public MismatchedTenantIdResolution NewEntityMismatchedTenantIdBehavior { get; set; } = MismatchedTenantIdResolution.ThrowException;
    public bool AllowScopeCreationForNonActiveTenants { get; set; } = false;
}
