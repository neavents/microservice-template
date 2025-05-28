using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.MultiTenancy.Implementations;

public partial class TenantOperationScope
{
    private const int ClassId = 85;
    private const int BaseEventId = Logging.MultiTenancyBaseEventId + (ClassId * Logging.IncrementPerClass);

    public const int EvtOperationScopeDisposed = BaseEventId + (0 * Logging.IncrementPerLog);

    [LoggerMessage(
        EventId = EvtOperationScopeDisposed,
        Level = LogLevel.Information,
        Message = "Tenant operation scope disposed. Tenant context restored. Restored Tenant: {RestoredTenantId}, Was Active In Scope: {ActiveTenantId}"
    )]
    public static partial void LogOperationScopeDisposed(ILogger logger, string restoredTenantId, string activeTenantId);

}
