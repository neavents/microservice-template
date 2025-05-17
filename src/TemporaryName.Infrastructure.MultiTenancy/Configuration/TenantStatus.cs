namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public enum TenantStatus
{
    Unknown = 0, 
    Provisioning = 1, 
    Active = 2, 
    Suspended = 3, 
    Deactivated = 4, 
    Archived = 5,
    Deleted = 6
}
