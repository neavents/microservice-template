using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Outbox.EFCore;

public class Logging
{
    public const int ProjectId = 11;
    public const int OutboxEfCoreBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Outbox.EFCore";
}
