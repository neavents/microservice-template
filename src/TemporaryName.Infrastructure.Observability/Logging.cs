using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Observability;

public class Logging
{
    public const int ProjectId = 10;
    public const int ObservabilityBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Observability";
}
