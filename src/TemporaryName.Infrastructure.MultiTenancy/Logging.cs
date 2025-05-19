using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.MultiTenancy;

internal class Logging
{
    public const int ProjectId = 15;
    public const int MultiTenancyBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    
}
