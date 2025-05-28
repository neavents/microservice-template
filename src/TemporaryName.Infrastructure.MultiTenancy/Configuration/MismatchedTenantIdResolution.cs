namespace TemporaryName.Infrastructure.MultiTenancy.Configuration;

public enum MismatchedTenantIdResolution
{
    ThrowException = 0,
    OverrideWithContextTenantId = 1
}
