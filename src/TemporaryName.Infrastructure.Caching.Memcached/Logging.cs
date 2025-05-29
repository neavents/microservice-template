using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Caching.Memcached;

public class Logging
{
    public const int ProjectId = 4;
    public const int CachingMemcachedBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Caching.Memcached";
}
