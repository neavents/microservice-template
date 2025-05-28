namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public enum TenantDataIsolationMode
{
    SharedDatabaseSharedSchema = 0, 
    SharedDatabaseDedicatedSchema = 1, 
    DedicatedDatabase = 2, 
    Hybrid = 3
}
