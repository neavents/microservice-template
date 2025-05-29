using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Caching.Redis;

public class Logging
{
    public const int ProjectId = 5;

    public const int CachingRedisBaseEventId = ProjectId * BaseLogging.IncrementPerProject;

    public const int IncrementPerClass = 1_000;

    public const int IncrementPerLog = 10;

    public const string ProjectName = "TemporaryName.Infrastructure.Caching.Redis";
}
