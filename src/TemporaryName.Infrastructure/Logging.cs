using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure;

public class Logging
{
    public const int ProjectId = 17;
    public const int InfrastructureBaseId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "Infrastructure.Caching.Memcached";
}
